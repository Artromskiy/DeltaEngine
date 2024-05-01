using Arch.Core;
using Collections.Pooled;
using Delta.ECS.Components;
using Delta.Rendering;
using Delta.Rendering.Internal;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Delta.ECS;
internal class Batcher : ISystem
{
    private readonly Queue<uint> _free;

    /// <summary>
    /// Contains matrices with world position of each <see cref="Render"/>
    /// </summary>
    public readonly GpuArray<Matrix4x4> trs;
    /// <summary>
    /// Contains indices to elements of <see cref="trs"/> ordered by <see cref="Render"/>
    /// </summary>
    public readonly GpuArray<uint> trsIds; // send to compute to sort trs on device

    // TODO
    private readonly GpuArray<uint> _trsTransfer; // send to compute to transfer new trs from host to device

    private readonly PooledSet<uint> _forceTrsWrite;
    private readonly List<uint> _transferIndicesSet;

    private readonly World _world;

    private static readonly QueryDescription _removeDescription = new QueryDescription().WithAll<RendId>().WithNone<Render>();
    private static readonly QueryDescription _addDescription = new QueryDescription().WithAll<Render>().WithNone<RendId>();
    private static readonly QueryDescription _changeDescription = new QueryDescription().WithAll<Render, RendId, DirtyFlag<Render>>();
    private static readonly QueryDescription _renderDescription = new QueryDescription().WithAll<Render, RendId>();

    private static readonly QueryDescription _trsDescriptionTransform = new QueryDescription().WithAll<Render, RendId, Transform>();
    private static readonly QueryDescription _trsDescriptionParent = new QueryDescription().WithAll<Render, RendId, ChildOf>().WithNone<Transform>();

    private readonly RenderGroupData _rendGroupData = new();

    private readonly PooledList<(Render rend, uint count)> _renders = [];

    private readonly ISystem[] systems;

    private const bool ForceWrites = true;

    public Batcher(World world, RenderBase renderBase)
    {
        _world = world;
        trsIds = new GpuArray<uint>(renderBase, 1);
        trs = new GpuArray<Matrix4x4>(renderBase, 1);

        _trsTransfer = new GpuArray<uint>(renderBase, 1);

        _transferIndicesSet = [];
        _forceTrsWrite = [];
        _free = new Queue<uint>((int)trs.Length);
        for (uint i = 0; i < trs.Length; i++)
            _free.Enqueue(i);
        systems =
        [
             new RemoveRender(_world, _free, _rendGroupData),
             new AddRender(_world, _free, _rendGroupData, _forceTrsWrite),
             new ChangeRender(_world, _rendGroupData),
             new SortWriteRender(_world, trsIds, _rendGroupData),
             new WriteTrs(_world, trs, _transferIndicesSet, _forceTrsWrite)
        ];
    }

    public void Execute()
    {
        _forceTrsWrite.Clear();
        _forceTrsWrite.TrimExcess(); // we assume it's better to free this, as large changes will create large internal array
        _transferIndicesSet.Clear();

        BufferResize();

        foreach (var item in systems)
            using (item as IDisposable)
                item.Execute();
    }

    public ReadOnlySpan<(Render rend, uint count)> GetRendGroups()
    {
        _renders.Clear();
        foreach (var item in _rendGroupData.sortedRendToGroup)
            _renders.Add((item.Key, _rendGroupData.groupToCount[item.Value]));
        return _renders.Span;
    }

    /// <summary>
    /// Ensures capacity of buffers to fit new <see cref="Render"/>s
    /// </summary>
    private void BufferResize()
    {
        var countToAdd = _world.CountEntities(_addDescription);
        var countToRemove = _world.CountEntities(_removeDescription);
        var wholeFree = _free.Count + countToRemove;
        var delta = countToAdd - wholeFree;
        if (delta > 0)
        {
            var length = trs.Length;
            var newLength = BitOperations.RoundUpToPowerOf2((uint)(length + delta));
            trs.Resize(newLength);
            trsIds.Resize(newLength);
            _free.EnsureCapacity((int)newLength);
            for (uint i = length; i < newLength; i++)
                _free.Enqueue(i);
        }
    }

    private readonly struct RemoveRender(World world, Queue<uint> freeIds, RenderGroupData rendGroupData) : ISystem
    {
        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            if (!world.Has(_removeDescription))
                return;
            var remover = new InlineRemover(freeIds, rendGroupData);
            world.InlineQuery<InlineRemover, RendId, RenderGroup>(_removeDescription, ref remover);
            world.Remove<RendId, RenderGroup>(_removeDescription);
        }

        private readonly struct InlineRemover(Queue<uint> free, RenderGroupData rendGroupData)
            : IForEach<RendId, RenderGroup>
        {
            [MethodImpl(Inl)]
            public readonly void Update(ref RendId id, ref RenderGroup group)
            {
                rendGroupData.Remove(group);
                free.Enqueue(id.trsId);
            }
        }
    }

    private readonly struct AddRender(World world, Queue<uint> free,
        RenderGroupData rendGroupData,
        PooledSet<uint> forceUpdate) : ISystem
    {
        private static readonly QueryDescription _addTag = new QueryDescription().WithAll<AddTag>();
        private readonly struct AddTag();

        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            if (!world.Has(_addDescription))
                return;
            // Add not yet registered entities
            // We also tagging all not registered entities
            // and then remove this tag, as it's faster than CommandBuffer
            world.Add<AddTag, RendId, RenderGroup>(_addDescription);

            InlineAdder adder = new(free, rendGroupData, forceUpdate);
            world.InlineQuery<InlineAdder, Render, RendId, RenderGroup>(_addTag, ref adder);
            world.Remove<AddTag>(_addTag);
        }

        private struct InlineAdder(
            Queue<uint> free,
            RenderGroupData rendGroupData,
            PooledSet<uint> forceUpdate)
            : IForEach<Render, RendId, RenderGroup>
        {
            [MethodImpl(Inl)]
            public readonly void Update(ref Render render, ref RendId id, ref RenderGroup group)
            {
                uint index = free.Dequeue();
                group = rendGroupData.Add(ref render);
                id = new RendId(index);
                forceUpdate.Add(index);
            }
        }
    }

    private readonly struct ChangeRender(World world, RenderGroupData rendGroupData) : ISystem
    {
        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            if (!world.Has(_changeDescription))
                return;
            ChangeWriter writer = new(rendGroupData);
            world.InlineQuery<ChangeWriter, Render, RenderGroup>(_changeDescription, ref writer);
        }

        private readonly struct ChangeWriter(RenderGroupData rendGroupData)
            : IForEach<Render, RenderGroup>
        {
            [MethodImpl(Inl)]
            public readonly void Update(ref Render render, ref RenderGroup group)
            {
                rendGroupData.Remove(group);
                group = rendGroupData.Add(ref render);
            }
        }
    }

    private readonly struct SortWriteRender(World world,
        GpuArray<uint> rendersArray,
        RenderGroupData rendGroupData) : ISystem
    {
        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            var offsets = ArrayPool<uint>.Shared.Rent(rendGroupData.rendToGroup.Count);
            foreach (var item in rendGroupData.groupToCount)
                offsets[item.Key.id] = item.Value;
            uint index = 0;
            foreach (var item in rendGroupData.sortedRendToGroup)
            {
                var tmp = offsets[item.Value.id];
                offsets[item.Value.id] = index;
                index += tmp;
            }
            RenderWriter writer = new(rendersArray.GetWriter(), offsets);
            world.InlineQuery<RenderWriter, RenderGroup, RendId>(_renderDescription, ref writer);
            ArrayPool<uint>.Shared.Return(offsets);
        }

        private readonly struct RenderWriter(GpuArray<uint>.Writer writer, uint[] offsets) :
            IForEach<RenderGroup, RendId>
        {
            [MethodImpl(Inl)]
            public readonly void Update(ref RenderGroup group, ref RendId id) => writer[offsets[group.id]++] = id.trsId;
        }
    }

    private readonly struct WriteTrs(World world, GpuArray<Matrix4x4> trs, List<uint> trsTransferIndices, PooledSet<uint> forceWrite) : ISystem
    {
        private readonly ThreadLocal<PooledList<uint>> _threadTransfer = new(() => new PooledList<uint>(ClearMode.Never), true);

        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            var writer = trs.GetWriter();
            WorldContext ctx = new(world);
            TrsWriterWithTransform trsWithTransform = new(ctx, writer, _threadTransfer, forceWrite);
            TrsWriterWithParent trsWithParent = new(ctx, writer, _threadTransfer, forceWrite);
            world.InlineParallelEntityQuery<TrsWriterWithTransform, RendId>(_trsDescriptionTransform, ref trsWithTransform);
            world.InlineParallelEntityQuery<TrsWriterWithParent, RendId>(_trsDescriptionParent, ref trsWithParent);
            foreach (var item in _threadTransfer.Values)
            {
                trsTransferIndices.AddRange(item.Span);
                item.Clear();
            }
        }

        private readonly struct TrsWriterWithTransform(WorldContext ctx, GpuArray<Matrix4x4>.Writer trsArray,
            ThreadLocal<PooledList<uint>> transfer, PooledSet<uint> forceWrite) :
            IForEachWithEntity<RendId>
        {
            [MethodImpl(Inl)]
            public readonly void Update(Entity entity, ref RendId rendToId)
            {
                if (!ForceWrites && !forceWrite.Contains(rendToId.trsId) && !ctx.HasParent<DirtyFlag<Transform>>(entity))
                    return;
                trsArray[rendToId.trsId] = ctx.GetWorldRecursive(entity);
                transfer.Value!.Add(rendToId.trsId);
            }
        }

        private readonly struct TrsWriterWithParent(WorldContext ctx, GpuArray<Matrix4x4>.Writer trsArray,
            ThreadLocal<PooledList<uint>> transfer, PooledSet<uint> forceWrite) :
            IForEachWithEntity<RendId>
        {
            [MethodImpl(Inl)]
            public readonly void Update(Entity entity, ref RendId rendToId)
            {
                if (!ForceWrites && !forceWrite.Contains(rendToId.trsId) && !ctx.HasParent<DirtyFlag<Transform>>(entity))
                    return;
                trsArray[rendToId.trsId] = ctx.GetParentWorldMatrix(entity);
                transfer.Value!.Add(rendToId.trsId);
            }
        }
    }

    /// <summary>
    /// Encapsulates containers with info about <see cref="Render"/>s ordering,
    /// dependent <see cref="RenderGroup"/>s and count per each <see cref="RenderGroup"/>
    /// </summary>
    private readonly struct RenderGroupData()
    {
        public readonly Dictionary<Render, RenderGroup> rendToGroup = [];
        public readonly SortedDictionary<Render, RenderGroup> sortedRendToGroup = [];
        public readonly Dictionary<RenderGroup, uint> groupToCount = [];

        /// <summary>
        /// Creates new <see cref="RenderGroup"/> if not present, increments count of entities in this group
        /// </summary>
        /// <param name="render"></param>
        /// <returns>Group to which <paramref name="render"/> corresponds</returns>
        public readonly RenderGroup Add(ref Render render)
        {
            if (!rendToGroup.TryGetValue(render, out var group))
            {
                sortedRendToGroup[render] = rendToGroup[render] = group = new RenderGroup((uint)rendToGroup.Count);
                groupToCount[group] = 0;
            }
            groupToCount[group]++;
            return group;
        }

        /// <summary>
        /// Decrements count of entities in <paramref name="group"/>
        /// </summary>
        /// <param name="group"></param>
        public readonly void Remove(RenderGroup group) => groupToCount[group]--;
    }

    /// <summary>
    /// Specifies unique id of <see cref="Render"/>.
    /// This value used directly to update TRS buffer
    /// </summary>
    /// <param name="trsId"></param>
    private readonly struct RendId(uint trsId)
    {
        public readonly uint trsId = trsId;
    }

    /// <summary>
    /// Specifies id of group to which <see cref="Render"/> refers.
    /// Used to improve performance when sorting <see cref="Render"/>s
    /// </summary>
    /// <param name="id"></param>
    internal readonly struct RenderGroup(uint id) : IEquatable<RenderGroup>
    {
        public readonly uint id = id;
        public bool Equals(RenderGroup other) => id == other.id;
        public override bool Equals(object? obj) => obj is RenderGroup group && Equals(group.id);
        public override int GetHashCode() => id.GetHashCode();
    }
}