using Arch.Core;
using DeltaEngine.Collections;
using DeltaEngine.Rendering;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DeltaEngine.ECS;
internal class GpuMappedSystem<N, T, K> : StorageDynamicArray<K>
    where K : unmanaged
    where T : IDirty<T>
    where N: struct, IGpuMapper<T,K>
{
    private readonly World _world;
    private readonly N _mapper;

    private bool[] _taken;
    private uint[] _count;

    private uint _lastFree;

    private uint[] _stack;
    private uint _stackSize;

    public int Count { get; private set; }

    private readonly QueryDescription _all = new QueryDescription().WithAll<T>();
    private readonly QueryDescription _withId = new QueryDescription().WithAll<T, VersId<T>>();

    public GpuMappedSystem(World world, RenderBase renderData) : base(renderData, 1)
    {
        _mapper = new();

        _lastFree = 1;
        _taken = [true];
        _count = [0];
        _stack = [];

        _world = world;

        var all = new QueryDescription().WithAll<T>();
        uint count = (uint)_world.CountEntities(all);
        uint newLength = BitOperations.RoundUpToPowerOf2(count);
        Resize(newLength);

        _world.Add<VersId<T>>(all);
        _world.Query(_withId, (ref T component, ref VersId<T> x) =>
        {
            x = Add(component);
        });
    }

    [MethodImpl(Inl)]
    public void UpdateDirty()
    {
        InlineUpdater updater = new(GetWriter());
        _world.InlineQuery<InlineUpdater, T, VersId<T>>(_withId, ref updater);
    }

    private readonly struct InlineUpdater(Writer writer) : IForEach<T, VersId<T>>
    {
        private readonly Writer _writer = writer;
        private readonly N _mapper = new();

        [MethodImpl(Inl)]
        public void Update(ref T component, ref VersId<T> vers)
        {
            if (component.IsDirty)
                _writer[vers.id] = _mapper.Map(ref component);
        }
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
        _count.CopyTo(newCount, 0);
        _stack.CopyTo(newStack, 0);

        _taken = newTaken;
        _count = newCount;
        _stack = newStack;
    }

    [MethodImpl(Inl)]
    private VersId<T> Add(T item)
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
        return new(index, _count[index]);
    }

    [MethodImpl(Inl)]
    private void Update(ref VersId<T> versId, ref T item)
    {
        CheckIndex(versId.id);
        CheckVersion(versId);
        this[versId.id] = _mapper.Map(ref item);
    }

    private void RemoveAt(uint index)
    {
        CheckIndex(index);
        this[index] = default;
        _taken[index] = false;
        _count[index]++;
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
        Debug.Assert(_stackSize < (uint)_stack.Length);
        _stack[_stackSize++] = index;
    }

    [MethodImpl(Inl)]
    private void CheckIndex(uint index)
    {
        if (index < 1 || index >= _lastFree || !_taken[index])
            Thrower.ThrowOnGet(index);
    }

    [MethodImpl(Inl)]
    private void CheckVersion(VersId<T> versId)
    {
        if (_count[versId.id] != versId.version)
            Thrower.ThrowOnVersion(versId.version);
    }


    private static class Thrower
    {
        public static void ThrowOnGet(uint index) => throw new ArgumentException($"Attempt to get not persistent item by index {index}");
        public static void ThrowOnVersion(uint version) => throw new ArgumentException($"Attempt to get not persistent item by version {version}");
    }
}
