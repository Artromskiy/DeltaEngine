using Arch.Core;
using Delta.ECS.Components;
using Delta.Rendering;
using Delta.Rendering.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Delta.ECS;
internal class GpuMapped<M, C, G> : GpuArray<G>
    where M : struct, IGpuMapper<C, G> // Mapper
    where G : unmanaged                // GpuStruct
{
    private static readonly M _mapper = new();

    private readonly World _world;

    private bool[] _taken;
    private uint[] _versn;

    private uint _lastFree;

    private uint[] _stack;
    private uint _stackSize;

    public int Count { get; private set; }

    private static readonly QueryDescription _all = new QueryDescription().WithAll<C>();
    private static readonly QueryDescription _withId = new QueryDescription().WithAll<C, VersId<C>>();
    private static readonly QueryDescription _withIdDirty = new QueryDescription().WithAll<C, VersId<C>, DirtyFlag<C>>();

    public GpuMapped(World world, RenderBase renderData) : base(renderData, (uint)world.CountEntities(_all))
    {
        _taken = new bool[Length];
        _taken[0] = true;
        _versn = new uint[Length];
        _stack = new uint[Length];

        _world = world;

        _world.Add<VersId<C>>(_all);
        Count = _world.CountEntities(_all);
        InlineCreator creator = new(GetWriter());
        _world.InlineQuery<InlineCreator, C, VersId<C>>(_withId, ref creator);
        Array.Fill(_taken, true, 1, Count);
        _lastFree = (uint)Count + 1;
    }

    public double GetFragmentationMetric()
    {
        int quality = 0;
        int freeSize = 0;
        int regionSize = 0;
        for (int i = 0; i < _lastFree; i++)
        {
            if (!_taken[i])
                regionSize++;
            if (_taken[i] && regionSize > 0)
            {
                quality += regionSize * regionSize;
                freeSize += regionSize;
            }
        }
        double qualityPercent = Math.Sqrt(quality) / freeSize;
        return 1 - (qualityPercent * qualityPercent);
    }

    [MethodImpl(Inl)]
    public (uint, uint) UpdateDirty()
    {
        InlineUpdater updater = new(GetWriter());
        _world.InlineParallelQuery<InlineUpdater, C, VersId<C>>(_withIdDirty, ref updater);
        InlineMinMax range = new();
        _world.InlineQuery<InlineMinMax, VersId<C>>(_withIdDirty, ref range);
        Flush(0, 0);
        return (0, 0);
    }

    [MethodImpl(Inl)]
    private void Grow() => Resize(Length * 2);

    [MethodImpl(NoInl)]
    private new void Resize(uint length)
    {
        var oldSize = Length;
        uint newSize = length;
        Debug.Assert(oldSize <= newSize);

        base.Resize(newSize);

        uint[] newStack = new uint[newSize];
        uint[] newCount = new uint[newSize];
        bool[] newTaken = new bool[newSize];

        _taken.CopyTo(newTaken, 0);
        _versn.CopyTo(newCount, 0);
        _stack.CopyTo(newStack, 0);

        _taken = newTaken;
        _versn = newCount;
        _stack = newStack;
    }

    [MethodImpl(Inl)]
    private VersId<C> Add(C item)
    {
        uint index;
        if (_stackSize > 0)
            index = PopStack();
        else
        {
            if (_lastFree == Length)
                Grow();
            index = _lastFree++;
        }
        _taken[index] = true;
        this[index] = _mapper.Map(ref item);
        Count++;
        return new(index, _versn[index]);
    }

    private void RemoveAt(uint index)
    {
        CheckIndex(index);
        this[index] = default;
        _taken[index] = false;
        _versn[index]++;
        PushStack(index);
        Count--;
    }

    [MethodImpl(Inl)]
    private uint PopStack()
    {
        Debug.Assert(_stackSize > 0);
        return _stack[--_stackSize];
    }

    [MethodImpl(Inl)]
    private uint PeekStack()
    {
        Debug.Assert(_stackSize > 0);
        return _stack[_stackSize - 1];
    }

    [MethodImpl(Inl)]
    private void PushStack(uint index)
    {
        Debug.Assert(index != 0 && index < Length);
        _stack[_stackSize++] = index;
    }

    [MethodImpl(Inl)]
    private void CheckIndex(uint index)
    {
        if (index == 0 || index >= _lastFree || !_taken[index])
            Thrower.ThrowOnGet(index);
    }

    [MethodImpl(Inl)]
    private void CheckVersion(VersId<C> versId)
    {
        if (_versn[versId.id] != versId.vs)
            Thrower.ThrowOnVersion(versId.vs);
    }


    private static class Thrower
    {
        public static void ThrowOnGet(uint index) => throw new ArgumentException($"Attempt to get not persistent item by index {index}");
        public static void ThrowOnVersion(uint version) => throw new ArgumentException($"Attempt to get not persistent item by version {version}");
    }

    private struct InlineCreator(Writer writer) : IForEach<C, VersId<C>>
    {
        private uint index = 0;
        public void Update(ref C cmp, ref VersId<C> vers)
        {
            var index = Interlocked.Increment(ref this.index);
            writer[index] = _mapper.Map(ref cmp);
            vers = new(index, 0);
        }
    }

    private struct InlineUpdater(Writer writer) : IForEach<C, VersId<C>>
    {
        [MethodImpl(Inl)]
        public readonly void Update(ref C cmp, ref VersId<C> vers)
        {
            writer[vers.id] = _mapper.Map(ref cmp);
        }
    }

    private struct InlineMinMax() : IForEach<VersId<C>>
    {
        private uint min = uint.MaxValue;
        private uint max = 0;
        public readonly uint Min => min;
        public readonly uint Max => max;
        public void Update(ref VersId<C> vers)
        {
            min = Math.Min(vers.id, min);
            max = Math.Max(vers.id, max);
        }
    }
}

public readonly struct DirectMapper<C> : IGpuMapper<C, C> where C : unmanaged
{
    [MethodImpl(Inl)]
    public readonly C Map(ref C value) => value;
}
internal class GpuMappedSystem<C>(World world, RenderBase renderData) : GpuMapped<DirectMapper<C>, C, C>(world, renderData) where C : unmanaged { }
