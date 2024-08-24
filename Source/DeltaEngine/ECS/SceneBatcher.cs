using Arch.Core;
using Arch.Core.Extensions;
using Collections.Pooled;
using Delta.ECS.Components;
using Delta.Rendering;
using Delta.Rendering.Collections;
using Delta.Runtime;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Delta.ECS;
internal class SceneBatcher : IRenderBatcher
{
    private readonly Queue<uint> _free;

    public GpuArray<GpuCameraData> Camera { get; private set; }
    public GpuArray<Matrix4x4> Transforms { get; private set; }
    public GpuArray<uint> TransformIds { get; private set; }

    // TODO
    private readonly GpuArray<uint> TransformIdsToTransfer; // send to compute to transfer new trs from host to device

    private readonly PooledSet<uint> _forceTrsWrite;
    private readonly List<uint> _transferIndicesSet;

    //private readonly World _world;

    private static readonly QueryDescription _cameraDescription = new QueryDescription().WithAll<Camera, Transform>();

    private static readonly QueryDescription _removeDescription = new QueryDescription().WithAll<RendId>().WithNone<Render>();
    private static readonly QueryDescription _addDescription = new QueryDescription().WithAll<Render>().WithNone<RendId>();
    private static readonly QueryDescription _changeDescription = new QueryDescription().WithAll<Render, RendId, DirtyFlag<Render>>();
    private static readonly QueryDescription _renderDescription = new QueryDescription().WithAll<Render, RendId>();

    private static readonly QueryDescription _trsDescriptionTransform = new QueryDescription().WithAll<Render, RendId, Transform>();
    private static readonly QueryDescription _trsDescriptionParent = new QueryDescription().WithAll<Render, RendId, ChildOf>().WithNone<Transform>();

    private readonly RenderGroupData _rendGroupData = new();

    private readonly List<(Render rend, uint count)> _renders = [];

    private const bool ForceWrites = true;
    private readonly RemoveRender _removeRender;
    private readonly AddRender _addRender;
    private readonly ChangeRender _changeRender;
    private readonly SortWriteRender _sortWriteRender;
    private readonly WriteTrs _writeTrs;
    private readonly WriteCamera _writeCamera;

    public SceneBatcher()
    {
        var renderBase = IRuntimeContext.Current.GraphicsModule.RenderData;
        Camera = new GpuArray<GpuCameraData>(renderBase, 1);
        TransformIds = new GpuArray<uint>(renderBase, 1);
        Transforms = new GpuArray<Matrix4x4>(renderBase, 1);

        TransformIdsToTransfer = new GpuArray<uint>(renderBase, 1);

        _transferIndicesSet = [];
        _forceTrsWrite = [];
        _free = new Queue<uint>((int)Transforms.Length);
        for (uint i = 0; i < Transforms.Length; i++)
            _free.Enqueue(i);

        _removeRender = new RemoveRender(_free, _rendGroupData);
        _addRender = new AddRender(_free, _rendGroupData, _forceTrsWrite);
        _changeRender = new ChangeRender(_rendGroupData);
        _sortWriteRender = new SortWriteRender(TransformIds, _rendGroupData);
        _writeTrs = new WriteTrs(Transforms, _transferIndicesSet, _forceTrsWrite);
        _writeCamera = new WriteCamera(Camera);
    }

    public void Dispose()
    {
        Camera.Dispose();
        TransformIds.Dispose();
        Transforms.Dispose();
        TransformIdsToTransfer.Dispose();
        _forceTrsWrite.Dispose();
        _renders.Clear();
    }

    private void OnSceneChanged()
    {
        _rendGroupData.Clear();
        _free.Clear();
        for (uint i = 0; i < Transforms.Length; i++)
            _free.Enqueue(i);
    }

    public void Execute()
    {
        _forceTrsWrite.Clear();
        _forceTrsWrite.TrimExcess(); // we assume it's better to free this, as large changes will create large internal array
        _transferIndicesSet.Clear();

        BufferResize();

        _removeRender.Execute();
        _addRender.Execute();
        _changeRender.Execute();
        _sortWriteRender.Execute();
        _writeTrs.Execute();
        _writeCamera.Execute();
    }

    public ReadOnlySpan<(Render rend, uint count)> RendGroups
    {
        get
        {
            _renders.Clear();
            foreach (var item in _rendGroupData.sortedRendToGroup)
                _renders.Add((item.Key, _rendGroupData.groupToCount[item.Value]));
            return CollectionsMarshal.AsSpan(_renders);
        }
    }

    /// <summary>
    /// Ensures capacity of buffers to fit new <see cref="Render"/>s
    /// </summary>
    private void BufferResize()
    {
        var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
        var countToAdd = world.CountEntities(_addDescription);
        var countToRemove = world.CountEntities(_removeDescription);
        var wholeFree = _free.Count + countToRemove;
        var delta = countToAdd - wholeFree;
        if (delta > 0)
        {
            var length = Transforms.Length;
            var newLength = BitOperations.RoundUpToPowerOf2((uint)(length + delta));
            Transforms.Resize(newLength);
            TransformIds.Resize(newLength);
            _free.EnsureCapacity((int)newLength);
            for (uint i = length; i < newLength; i++)
                _free.Enqueue(i);
        }
    }

    private readonly struct RemoveRender(Queue<uint> freeIds, RenderGroupData rendGroupData) : ISystem
    {
        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
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

    private readonly struct AddRender(Queue<uint> free,
        RenderGroupData rendGroupData,
        PooledSet<uint> forceUpdate) : ISystem
    {
        private static readonly QueryDescription _addTag = new QueryDescription().WithAll<AddTag>();
        private readonly struct AddTag();

        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
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

    private readonly struct ChangeRender(RenderGroupData rendGroupData) : ISystem
    {
        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
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

    private readonly struct SortWriteRender(
        GpuArray<uint> rendersArray,
        RenderGroupData rendGroupData) : ISystem
    {
        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
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
            RenderWriter writer = new(rendersArray.Writer, offsets);
            world.InlineQuery<RenderWriter, RenderGroup, RendId>(_renderDescription, ref writer);
            ArrayPool<uint>.Shared.Return(offsets);
        }

        private readonly struct RenderWriter(GpuArray<uint>.GpuWriter writer, uint[] offsets) :
            IForEach<RenderGroup, RendId>
        {
            [MethodImpl(Inl)]
            public readonly void Update(ref RenderGroup group, ref RendId id) => writer[offsets[group.id]++] = id.trsId;
        }
    }

    private readonly struct WriteTrs(GpuArray<Matrix4x4> trs, List<uint> trsTransferIndices, PooledSet<uint> forceWrite) : ISystem
    {
        private readonly ThreadLocal<PooledList<uint>> _threadTransfer = new(() => new PooledList<uint>(ClearMode.Never), true);

        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            var writer = trs.Writer;
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

        private readonly struct TrsWriterWithTransform(WorldContext ctx, GpuArray<Matrix4x4>.GpuWriter trsArray,
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

        private readonly struct TrsWriterWithParent(WorldContext ctx, GpuArray<Matrix4x4>.GpuWriter trsArray,
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

    private readonly struct WriteCamera(GpuArray<GpuCameraData> _cameraArray) : ISystem
    {
        public void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            var writer = _cameraArray.Writer;
            if (world.CountEntities(_cameraDescription) != 0)
                world.Query(_cameraDescription, (entity) => writer[0] = GetCameraData(entity));
            else
                writer[0] = GpuCameraData.DefaultCamera();
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
        public readonly RenderGroup Add(ref readonly Render render)
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
        public readonly void Clear()
        {
            rendToGroup.Clear();
            sortedRendToGroup.Clear();
            groupToCount.Clear();
        }
    }

    /// <summary>
    /// Specifies unique id of <see cref="Render"/>.
    /// </summary>
    /// <param name="trsId">index of Transform in TRS buffer</param>
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

    private static GpuCameraData GetCameraData(Entity entity)
    {
        var matrix = entity.GetWorldMatrix();
        var camera = entity.Get<Camera>();
        return new GpuCameraData(camera, matrix);
    }
}