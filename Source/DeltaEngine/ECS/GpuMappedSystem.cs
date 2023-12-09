using Arch.Core;
using Arch.Core.Extensions;
using DeltaEngine.Collections;
using DeltaEngine.Rendering;
using System;
using System.Runtime.CompilerServices;

namespace DeltaEngine.ECS;
internal class GpuMappedSystem<T,K> : StorageDynamicArray<K> where K : unmanaged
{
    private readonly World _world;
    private readonly IGpuMapper<T, K> _mapper;

    private bool[] _taken;
    private uint[] _count;

    private uint _lastFree;

    private uint[] _stack;
    private uint _stackSize;

    public GpuMappedSystem(World world, IGpuMapper<T,K> mapper, RenderBase renderData) : base(renderData, 1)
    {
        _lastFree = 1;
        _taken = [true];
        _count = [0];
        _stack = [];

        _mapper = mapper;
        _world = world;

        var all = new QueryDescription().WithAll<T>();
        var withId = new QueryDescription().WithAll<T, VersId<T>>();
        _world.Add<VersId<T>>(all);
        _world.Query(withId, (ref T component, ref VersId <T> x) =>
        {
            x = Next();
            Add(component);
        });
        _world.SubscribeComponentAdded<T>(OnComponentAdded);
        _world.SubscribeComponentRemoved<T>(OnComponentRemoved);
        _world.SubscribeComponentSet<T>(OnComponentChanged);
    }

    public void UpdateDirtyValues()
    {
        var onlyDirty = new QueryDescription().WithAll<DirtyFlag<T>>();
        var query = new QueryDescription().WithAll<T, VersId<T>, DirtyFlag<T>>();
        _world.ParallelQuery(query, (ref T component, ref VersId<T> vers) =>
        {
            Update(vers, component);
        });
        _world.Remove<DirtyFlag<T>>(onlyDirty);
    }

    private void OnComponentAdded(in Entity entity, ref T component)
    {
        var nextId = Next();
        _world.Add(entity, in nextId);
        Add(component);
    }

    private void OnComponentRemoved(in Entity entity, ref T component)
    {
        ref var verId = ref entity.TryGetRef<VersId<T>>(out var has);
        Debug.Assert(has);
        RemoveAt(verId.id);
        _world.Remove<VersId<T>>(entity);
        _world.Remove<DirtyFlag<T>>(entity);
    }

    private void OnComponentChanged(in Entity entity, ref T component)
    {
        _world.AddOrGet<DirtyFlag<T>>(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Update(VersId<T> versId, T item)
    {
        CheckIndex(versId.id);
        CheckVersion(versId);
        this[versId.id] = _mapper.Map(item);
    }

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
        this[index] = _mapper.Map(item);
        return new(index, _count[index]);
    }


    private void Grow()
    {
        uint newSize = Length == 0 ? 1 : Length * 2;

        Resize(newSize);

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

    private void RemoveAt(uint index)
    {
        CheckIndex(index);
        this[index] = default;
        _taken[index] = false;
        _count[index]++;
        PushStack(index);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private VersId<T> Next()
    {
        var index = _stackSize > 0 ? PeekStack() : _lastFree;
        return new(index, _count[index]);
    }

    private uint PopStack()
    {
        Debug.Assert(_stackSize > 0);
        return _stack[--_stackSize];
    }

    private uint PeekStack()
    {
        Debug.Assert(_stackSize > 0);
        return _stack[_stackSize - 1];
    }

    private void PushStack(uint index)
    {
        Debug.Assert(_stackSize < (uint)_stack.Length);
        _stack[_stackSize++] = index;
        VersId<T> v = new(0, 0);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckIndex(uint index)
    {
        if (index < 1 || index >= _lastFree || !_taken[index])
            Thrower.ThrowOnGet(index);
    }
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
