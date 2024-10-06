using Arch.Core;
using Collections.Pooled;
using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering.Collections;
using Delta.Runtime;
using Delta.Utilities;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Delta.Rendering;

internal class SceneBatcher : GenericBatcher<Matrix4x4, int, int, GpuCameraData>
{
    private readonly Stack<int> _free;

    private readonly int[][] _bindings = [[0, 1], [0], [0]];
    public override JaggedSpan<int> DescriptorSetBindings => _bindings;

    private GpuArray<Matrix4x4> Trs => _bufferT0;
    private GpuArray<int> TrsIds => _bufferT1;
    private GpuArray<int> Materials => _bufferT2;
    private GpuArray<GpuCameraData> Camera => _bufferT3;
    // TODO
    private readonly GpuArray<int> TransformIdsToTransfer; // send to compute to transfer new trs from host to device

    private readonly PooledSet<int> _forceTrsWrite;
    private readonly List<int> _transferIndicesSet;

    private static readonly QueryDescription _cameraDescription = new QueryDescription().WithAll<Camera, Transform>();

    private static readonly QueryDescription _removeEntityDescription = new QueryDescription().WithAll<Render, RendId, RenderGroup, DestroyFlag>();
    private static readonly QueryDescription _removeRenderDescription = new QueryDescription().WithAll<RendId, RenderGroup>().WithNone<Render>();
    private static readonly QueryDescription _addDescription = new QueryDescription().WithAll<Render>().WithNone<RendId, RenderGroup>();
    private static readonly QueryDescription _changeDescription = new QueryDescription().WithAll<Render, RendId, RenderGroup, DirtyFlag<Render>>();
    private static readonly QueryDescription _renderDescription = new QueryDescription().WithAll<Render, RendId, RenderGroup>();

    private static readonly QueryDescription _trsDescriptionTransform = new QueryDescription().WithAll<Render, RendId, RenderGroup, Transform>();
    private static readonly QueryDescription _trsDescriptionParent = new QueryDescription().WithAll<Render, RendId, RenderGroup, ChildOf>().WithNone<Transform>();

    private readonly RenderGroupData _rendGroupData = new();

    private readonly List<(Render rend, int count)> _renders = [];

    private const bool ForceWrites = true;
    private readonly RemoveEntity _removeEntity;
    private readonly RemoveRender _removeRender;
    private readonly AddRender _addRender;
    private readonly ChangeRender _changeRender;
    private readonly SortWriteRender _sortWriteRender;
    private readonly WriteTrs _writeTrs;
    private readonly WriteCamera _writeCamera;

    public SceneBatcher()
    {
        TransformIdsToTransfer = new(1);

        _transferIndicesSet = [];
        _forceTrsWrite = [];
        _free = new Stack<int>(Trs.Length);
        for (int i = Trs.Length - 1; i >= 0; i--)
            _free.Push(i);

        _removeEntity = new RemoveEntity(_free, _rendGroupData);
        _removeRender = new RemoveRender(_free, _rendGroupData);
        _addRender = new AddRender(_free, _rendGroupData, _forceTrsWrite);
        _changeRender = new ChangeRender(_rendGroupData);
        _sortWriteRender = new SortWriteRender(TrsIds, _rendGroupData);
        _writeTrs = new WriteTrs(Trs, _transferIndicesSet, _forceTrsWrite);
        _writeCamera = new WriteCamera(Camera);
    }

    public override void Dispose()
    {
        Camera.Dispose();
        TrsIds.Dispose();
        Trs.Dispose();
        TransformIdsToTransfer.Dispose();
        _forceTrsWrite.Dispose();
        _renders.Clear();
    }

    private void OnSceneChanged()
    {
        // _rendGroupData.Clear();
        // _free.Clear();
        // for (uint i = 0; i < Transforms.Length; i++)
        //     _free.Enqueue(i);
    }

    public override void Execute()
    {
        _forceTrsWrite.Clear();
        _forceTrsWrite.TrimExcess(); // we assume it's better to free this, as large changes will create large internal array
        _transferIndicesSet.Clear();

        BufferResize();

        _removeEntity.Execute();
        _removeRender.Execute();
        _addRender.Execute();
        _changeRender.Execute();
        _sortWriteRender.Execute();
        _writeTrs.Execute();
        _writeCamera.Execute();
    }

    public override ReadOnlySpan<(Render rend, int count)> RendGroups
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
        var countToRemove = world.CountEntities(_removeRenderDescription);
        var wholeFree = _free.Count + countToRemove;
        var delta = countToAdd - wholeFree;
        if (delta > 0)
        {
            var length = Trs.Length;
            var newLength = (int)BitOperations.RoundUpToPowerOf2((uint)(length + delta));
            Trs.Resize(newLength);
            TrsIds.Resize(newLength);
            _free.EnsureCapacity(newLength);
            for (int i = newLength - 1; i >= length; i--)
                _free.Push(i);
        }
    }

    private readonly struct RemoveEntity(Stack<int> freeIds, RenderGroupData rendGroupData) : ISystem
    {
        [Imp(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            if (!world.Has(_removeEntityDescription))
                return;
            var remover = new InlineRemover(freeIds, rendGroupData);
            world.InlineQuery<InlineRemover, RendId, RenderGroup>(_removeEntityDescription, ref remover);
            world.Remove<RendId, RenderGroup, Render>(_removeEntityDescription);
        }

        private readonly struct InlineRemover(Stack<int> free, RenderGroupData rendGroupData)
            : IForEach<RendId, RenderGroup>
        {
            [Imp(Inl)]
            public readonly void Update(ref RendId id, ref RenderGroup group)
            {
                rendGroupData.Remove(group);
                free.Push(id.trsId);
            }
        }
    }

    private readonly struct RemoveRender(Stack<int> freeIds, RenderGroupData rendGroupData) : ISystem
    {
        [Imp(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            if (!world.Has(_removeRenderDescription))
                return;
            var remover = new InlineRemover(freeIds, rendGroupData);
            world.InlineQuery<InlineRemover, RendId, RenderGroup>(_removeRenderDescription, ref remover);
            world.Remove<RendId, RenderGroup>(_removeRenderDescription);
        }

        private readonly struct InlineRemover(Stack<int> free, RenderGroupData rendGroupData)
            : IForEach<RendId, RenderGroup>
        {
            [Imp(Inl)]
            public readonly void Update(ref RendId id, ref RenderGroup group)
            {
                rendGroupData.Remove(group);
                free.Push(id.trsId);
            }
        }
    }

    private readonly struct AddRender(Stack<int> free,
        RenderGroupData rendGroupData,
        PooledSet<int> forceUpdate) : ISystem
    {
        private static readonly QueryDescription _addTag = new QueryDescription().WithAll<AddTag>();
        private readonly struct AddTag();

        [Imp(Inl)]
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
            Stack<int> free,
            RenderGroupData rendGroupData,
            PooledSet<int> forceUpdate)
            : IForEach<Render, RendId, RenderGroup>
        {
            [Imp(Inl)]
            public readonly void Update(ref Render render, ref RendId id, ref RenderGroup group)
            {
                render._shader = render.material.Null ? new() : render.material.GetAsset().shader;

                int index = free.Pop();
                group = rendGroupData.Add(ref render);
                id = new RendId(index);
                forceUpdate.Add(index);
            }
        }
    }

    private readonly struct ChangeRender(RenderGroupData rendGroupData) : ISystem
    {
        [Imp(Inl)]
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
            [Imp(Inl)]
            public readonly void Update(ref Render render, ref RenderGroup group)
            {
                render._shader = render.material.Null ? new() : render.material.GetAsset().shader;

                rendGroupData.Remove(group);
                group = rendGroupData.Add(ref render);
            }
        }
    }

    private readonly struct SortWriteRender(
        GpuArray<int> rendersArray,
        RenderGroupData rendGroupData) : ISystem
    {
        [Imp(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            var offsetsCount = rendGroupData.rendToGroup.Count;
            //Debug.Assert(offsetsCount != 0);
            var offsets = ArrayPool<int>.Shared.Rent(offsetsCount);
            foreach (var item in rendGroupData.groupToCount)
                offsets[item.Key.id] = item.Value;
            int index = 0;
            foreach (var item in rendGroupData.sortedRendToGroup)
            {
                var tmp = offsets[item.Value.id];
                offsets[item.Value.id] = index;
                index += tmp;
            }
            RenderWriter writer = new(rendersArray.Writer, offsets);
            world.InlineQuery<RenderWriter, RenderGroup, RendId>(_renderDescription, ref writer);
            ArrayPool<int>.Shared.Return(offsets);
        }

        private readonly struct RenderWriter(PointerWriter<int> writer, int[] offsets) :
            IForEach<RenderGroup, RendId>
        {
            [Imp(Inl)]
            public readonly void Update(ref RenderGroup group, ref RendId id)
            {
                var offset = offsets[group.id]++;
                writer[offset] = id.trsId;
            }
        }
    }

    private readonly struct WriteTrs(GpuArray<Matrix4x4> trs, List<int> trsTransferIndices, PooledSet<int> forceWrite) : ISystem
    {
        private readonly ThreadLocal<PooledList<int>> _threadTransfer = new(() => new PooledList<int>(ClearMode.Never), true);

        [Imp(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            PointerWriter<Matrix4x4> writer = trs.Writer;
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

        private readonly struct TrsWriterWithTransform(WorldContext ctx, PointerWriter<Matrix4x4> trsArray,
            ThreadLocal<PooledList<int>> transfer, PooledSet<int> forceWrite) :
            IForEachWithEntity<RendId>
        {
            [Imp(Inl)]
            public readonly void Update(Entity entity, ref RendId rendId)
            {
                if (!ForceWrites && !forceWrite.Contains(rendId.trsId) && !ctx.HasParent<DirtyFlag<Transform>>(entity))
                    return;
                trsArray[rendId.trsId] = ctx.GetWorldRecursive(entity);
                transfer.Value!.Add(rendId.trsId);
            }
        }

        private readonly struct TrsWriterWithParent(WorldContext ctx, PointerWriter<Matrix4x4> trsArray,
            ThreadLocal<PooledList<int>> transfer, PooledSet<int> forceWrite) :
            IForEachWithEntity<RendId>
        {
            [Imp(Inl)]
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
        public readonly Dictionary<RenderGroup, int> groupToCount = [];

        /// <summary>
        /// Creates new <see cref="RenderGroup"/> if not present, increments count of entities in this group
        /// </summary>
        /// <param name="render"></param>
        /// <returns>Group to which <paramref name="render"/> corresponds</returns>
        public readonly RenderGroup Add(ref readonly Render render)
        {
            if (!rendToGroup.TryGetValue(render, out var group))
            {
                group = new((uint)rendToGroup.Count);
                sortedRendToGroup[render] = rendToGroup[render] = group;
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
    [DebuggerDisplay("trsId = {trsId}")]
    private readonly struct RendId(int trsId)
    {
        public readonly int trsId = trsId;
    }

    /// <summary>
    /// Specifies id of group to which <see cref="Render"/> refers.
    /// Used to improve performance when sorting <see cref="Render"/>s
    /// </summary>
    /// <param name="id"></param>
    [DebuggerDisplay("id = {id}")]
    internal readonly struct RenderGroup(uint id) : IEquatable<RenderGroup>
    {
        public readonly uint id = id;
        public bool Equals(RenderGroup other) => id == other.id;
        public override bool Equals(object? obj) => obj is RenderGroup group && Equals(group.id);
        public override int GetHashCode() => id.GetHashCode();
    }

    private static GpuCameraData GetCameraData(Entity entity)
    {
        (float width, float height) = IRuntimeContext.Current.GraphicsModule.Size;
        var matrix = entity.GetWorldMatrix();
        var camera = entity.Get<Camera>();
        return new GpuCameraData(camera, matrix, width / height);
    }
}