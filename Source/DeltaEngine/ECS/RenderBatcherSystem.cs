using Arch.Core;
using Collections.Pooled;
using Delta.Rendering;
using JobScheduler;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Delta.ECS;
internal class RenderBatcherSystem
{
    private readonly Stack<uint> _free = [];
    private uint _lastIndex = 0;

    private readonly GpuArray<Matrix4x4> _trs;
    private readonly GpuArray<uint> _trsTransfer; // send to compute to transfer new trs from host to device
    private readonly GpuArray<uint> _trsSorted; // send to compute to sort trs on device

    private readonly PooledSet<uint> _forceTrsWrite;
    private readonly List<uint> _transferIndicesSet;

    private readonly World _world;

    private static readonly QueryDescription _removeDescription = new QueryDescription().WithAll<RendId>().WithNone<Render>();
    private static readonly QueryDescription _addDescription = new QueryDescription().WithAll<Render>().WithNone<RendId>();
    private static readonly QueryDescription _changeDescription = new QueryDescription().WithAll<Render, RendId, DirtyFlag<Render>>();
    private static readonly QueryDescription _renderDescription = new QueryDescription().WithAll<Render, RendId>();

    private static readonly QueryDescription _trsDescriptionTransform = new QueryDescription().WithAll<Render, RendId, Transform>();
    private static readonly QueryDescription _trsDescriptionParent = new QueryDescription().WithAll<Render, RendId, ChildOf>().WithNone<Transform>();

    private readonly RendGroupData _rendGroupData = new();

    public RenderBatcherSystem(World world, RenderBase renderBase)
    {
        _world = world;
        _trsSorted = new GpuArray<uint>(renderBase, 1);
        _trs = new GpuArray<Matrix4x4>(renderBase, 1);
        _trsTransfer = new GpuArray<uint>(renderBase, 1);
        _transferIndicesSet = [];
        _forceTrsWrite = [];
    }

    private readonly Stopwatch _trsWrite = new();
    private readonly Stopwatch _jobSetup = new();
    private readonly Stopwatch _jobWait = new();

    public TimeSpan TrsWriteMetric => _trsWrite.Elapsed;
    public TimeSpan JobSetupMetric => _jobSetup.Elapsed;
    public TimeSpan JobWaitMetric => _jobWait.Elapsed;

    public void ClearMetrics()
    {
        _trsWrite.Reset();
        _jobSetup.Reset();
        _jobWait.Reset();
    }

    public void Update()
    {
        _forceTrsWrite.Clear();
        _forceTrsWrite.TrimExcess(); // we assume it's better to free this, as large changes will create large internal array

        _transferIndicesSet.Clear();

        RemoveRendJob removeJob = new(_world, _free, _rendGroupData);
        removeJob.Execute();

        uint newLastIndex = EnsureBuffersCapacity();

        AddRendJob addJob = new(_world, _free, _rendGroupData, _forceTrsWrite, _lastIndex);
        addJob.Execute();
        _lastIndex = newLastIndex;

        ChangeRendJob changeJob = new(_world, _rendGroupData);
        changeJob.Execute();

        _jobSetup.Start();
        using SortWriteJob sortWriteJob = new(_world, _trsSorted.GetWriter(), _rendGroupData);
        var handle = JobScheduler.JobScheduler.Instance.Schedule(sortWriteJob);
        JobScheduler.JobScheduler.Instance.Flush();
        _jobSetup.Stop();

        _trsWrite.Start();
        using TrsWriteJob trsJob = new(_world, _trs.GetWriter(), _transferIndicesSet, _forceTrsWrite);
        trsJob.Execute();
        _trsWrite.Stop();

        _jobWait.Start();
        handle.Complete();
        _jobWait.Stop();
    }

    /// <summary>
    /// Ensures capacity of buffers to fit new <see cref="Render"/>s
    /// </summary>
    /// <returns><see cref="_lastIndex"/> needed to be applied after all new <see cref="Render"/> been setup</returns>
    private uint EnsureBuffersCapacity()
    {
        var countToAdd = _world.CountEntities(_addDescription);
        if (countToAdd > 0)
        {
            var wholeFree = _free.Count + (_trs.Length - _lastIndex);
            uint newLastIndex = (uint)(_lastIndex + Math.Max(0, countToAdd - wholeFree));
            if (wholeFree < countToAdd)
            {
                uint size = (uint)(_trs.Length + countToAdd - wholeFree);
                _trs.Resize(BitOperations.RoundUpToPowerOf2(size));
                _trsSorted.Resize(BitOperations.RoundUpToPowerOf2(size));
            }
            return newLastIndex;
        }
        return _lastIndex;
    }

    private readonly struct RemoveRendJob(World world, Stack<uint> free, RendGroupData rendGroupData) : IJob
    {
        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            if (!world.Has(_removeDescription))
                return;
            var remover = new InlineRemover(free, rendGroupData);
            world.InlineQuery<InlineRemover, RendId, RendGroup>(_removeDescription, ref remover);
            world.Remove<RendId, RendGroup>(_removeDescription);
        }

        private readonly struct InlineRemover(Stack<uint> free, RendGroupData rendGroupData)
            : IForEach<RendId, RendGroup>
        {
            [MethodImpl(Inl)]
            public readonly void Update(ref RendId id, ref RendGroup group)
            {
                rendGroupData.Remove(group);
                free.Push(id.trsId);
            }
        }
    }

    private readonly struct AddRendJob(World world, Stack<uint> free,
        RendGroupData rendGroupData,
        PooledSet<uint> forceUpdate, uint lastIndex) : IJob
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
            world.Add<AddTag, RendId, RendGroup>(_addDescription);

            InlineAdder adder = new(free, rendGroupData, forceUpdate, lastIndex);
            world.InlineQuery<InlineAdder, Render, RendId, RendGroup>(_addTag, ref adder);
            world.Remove<AddTag>(_addTag);
        }

        private struct InlineAdder(
            Stack<uint> free,
            RendGroupData rendGroupData,
            PooledSet<uint> forceUpdate, uint lastIndex)
            : IForEach<Render, RendId, RendGroup>
        {
            public uint lastIndex = lastIndex;
            [MethodImpl(Inl)]
            public void Update(ref Render render, ref RendId id, ref RendGroup group)
            {
                uint index = free.Count > 0 ? free.Pop() : lastIndex++;
                group = rendGroupData.Add(ref render);
                id = new RendId(index);
                forceUpdate.Add(index);
            }
        }
    }

    private readonly struct ChangeRendJob(World world, RendGroupData rendGroupData) : IJob
    {
        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            if (!world.Has(_changeDescription))
                return;
            ChangeWriter writer = new(rendGroupData);
            world.InlineQuery<ChangeWriter, Render, RendGroup>(_changeDescription, ref writer);
        }

        private readonly struct ChangeWriter(RendGroupData rendGroupData)
            : IForEach<Render, RendGroup>
        {
            [MethodImpl(Inl)]
            public readonly void Update(ref Render render, ref RendGroup group)
            {
                rendGroupData.Remove(group);
                group = rendGroupData.Add(ref render);
            }
        }
    }

    private readonly struct SortWriteJob(World world,
        GpuArray<uint>.Writer rendersArray,
        RendGroupData rendGroupData) : IJob, IDisposable
    {
        private readonly uint[] offsets = ArrayPool<uint>.Shared.Rent(rendGroupData.rendToGroup.Count);
        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            foreach (var item in rendGroupData.groupToCount)
                offsets[item.Key.id] = item.Value;
            uint index = 0;
            foreach (var item in rendGroupData.sortedRendToGroup)
            {
                var tmp = offsets[item.Value.id];
                offsets[item.Value.id] = index;
                index += tmp;
            }
            RenderWriter writer = new(rendersArray, offsets);
            world.InlineQuery<RenderWriter, RendGroup, RendId>(_renderDescription, ref writer);
        }

        private readonly struct RenderWriter(GpuArray<uint>.Writer writer, uint[] offsets) :
            IForEach<RendGroup, RendId>
        {
            [MethodImpl(Inl)]
            public readonly void Update(ref RendGroup group, ref RendId id) => writer[offsets[group.id]++] = id.trsId;
        }
        [MethodImpl(Inl)]
        public readonly void Dispose() => ArrayPool<uint>.Shared.Return(offsets);
    }

    private readonly struct TrsWriteJob(World world, GpuArray<Matrix4x4>.Writer trs, List<uint> trsTransferIndices, PooledSet<uint> forceWrite) : IJob, IDisposable
    {
        private readonly ThreadLocal<PooledList<uint>> _threadTransfer = new(() => new PooledList<uint>(ClearMode.Never), true);

        [MethodImpl(Inl)]
        public readonly void Execute()
        {
            TrsWriterWithTransform trsWithTransform = new(trs, _threadTransfer, forceWrite);
            TrsWriterWithParent trsWithParent = new(trs, _threadTransfer, forceWrite);
            world.InlineParallelEntityQuery<TrsWriterWithTransform, RendId>(_trsDescriptionTransform, ref trsWithTransform);
            world.InlineParallelEntityQuery<TrsWriterWithParent, RendId>(_trsDescriptionParent, ref trsWithParent);

            foreach (var item in _threadTransfer.Values)
                trsTransferIndices.AddRange(item.Span);
        }

        [MethodImpl(Inl)]
        public void Dispose()
        {
            foreach (var item in _threadTransfer.Values)
                item.Clear();
            _threadTransfer.Dispose();
        }

        private readonly struct TrsWriterWithTransform(GpuArray<Matrix4x4>.Writer trsArray,
            ThreadLocal<PooledList<uint>> transfer, PooledSet<uint> forceWrite) :
            IForEachWithEntity<RendId>
        {
            [MethodImpl(Inl)]
            public readonly void Update(Entity entity, ref RendId rendToId)
            {
                if (!forceWrite.Contains(rendToId.trsId) && !entity.HasParent<DirtyFlag<Transform>>())
                    return;
                trsArray[rendToId.trsId] = entity.GetWorldRecursive();
                transfer.Value!.Add(rendToId.trsId);
            }
        }

        private readonly struct TrsWriterWithParent(GpuArray<Matrix4x4>.Writer trsArray,
            ThreadLocal<PooledList<uint>> transfer, PooledSet<uint> forceWrite) :
            IForEachWithEntity<RendId>
        {
            [MethodImpl(Inl)]
            public readonly void Update(Entity entity, ref RendId rendToId)
            {
                if (!forceWrite.Contains(rendToId.trsId) && !entity.HasParent<DirtyFlag<Transform>>())
                    return;
                trsArray[rendToId.trsId] = entity.GetParentWorldMatrix();
                transfer.Value!.Add(rendToId.trsId);
            }
        }
    }


    /// <summary>
    /// Encapsulates containers with info about <see cref="Render"/>s ordering,
    /// dependent <see cref="RendGroup"/>s and count per each <see cref="RendGroup"/>
    /// </summary>
    private readonly struct RendGroupData()
    {
        public readonly Dictionary<Render, RendGroup> rendToGroup = [];
        public readonly SortedDictionary<Render, RendGroup> sortedRendToGroup = [];
        public readonly Dictionary<RendGroup, uint> groupToCount = [];

        /// <summary>
        /// Creates new <see cref="RendGroup"/> if not present, increments count of entities in this group
        /// </summary>
        /// <param name="render"></param>
        /// <returns>Group to which <paramref name="render"/> corresponds</returns>
        public readonly RendGroup Add(ref Render render)
        {
            if (!rendToGroup.TryGetValue(render, out var group))
            {
                sortedRendToGroup[render] = rendToGroup[render] = group = new RendGroup((uint)rendToGroup.Count);
                groupToCount.Add(group, 0);
            }
            groupToCount[group]++;
            return group;
        }

        /// <summary>
        /// Decrements count of entities in <paramref name="group"/>
        /// </summary>
        /// <param name="group"></param>
        public readonly void Remove(RendGroup group) => groupToCount[group]--;
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
    /// <param name="rehash"></param>
    private readonly struct RendGroup(uint rehash) : IEquatable<RendGroup>
    {
        public readonly uint id = rehash;
        public bool Equals(RendGroup other) => id == other.id;
        public override bool Equals(object? obj) => obj is RendGroup group && Equals(group.id);
        public override int GetHashCode() => id.GetHashCode();
    }
}