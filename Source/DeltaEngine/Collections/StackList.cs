using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeltaEngine.Collections;

public class StackList<T>
{
    private T[] _items;
    private bool[] _taken;
    private uint[] _count;

    private uint _lastFree;
    private uint _size;

    private uint[] _stack;
    private uint _stackSize;
    public uint Length => _size;


    public StackList()
    {
        _lastFree = 1;
        _size = 1;
        _items = [default!];
        _taken = [true];
        _count = [0];

        _stack = [];
    }

    public VersId<T> Add(T item)
    {
        uint index;
        if (_stackSize > 0)
            index = PopStack();
        else
        {
            if (_lastFree == _size)
                Grow();
            index = _lastFree++;
        }
        _taken[index] = true;
        _items[index] = item;
        return new(index, _count[index]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VersId<T> Next()
    {
        var index = _stackSize > 0 ? PeekStack() : _lastFree;
        return new(index, _count[index]);
    }

    internal ref T this[uint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            CheckIndex(index);
            return ref _items[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(VersId<T> versId, T item)
    {
        CheckIndex(versId.id);
        CheckVersion(versId);
        _items[versId.id] = item;
    }

    public void RemoveAt(uint index)
    {
        CheckIndex(index);
        _items[index] = default!;
        _taken[index] = false;
        _count[index]++;
        PushStack(index);
    }

    public unsafe void CopyTo(nint ptr)
    {
        ref byte bufferData = ref MemoryMarshal.GetArrayDataReference((Array)_items);
        Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(ptr.ToPointer()), ref bufferData, (uint)_size);
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

    public bool Has(int index) => index > 0 && index < _lastFree && _taken[index];

    private void Grow()
    {
        uint newSize = _size == 0 ? 1 : _size * 2;

        T[] newItems = new T[newSize];
        uint[] newStack = new uint[newSize];
        uint[] newCount = new uint[newSize];
        bool[] newTaken = new bool[newSize];

        _items.CopyTo(newItems, 0);
        _taken.CopyTo(newTaken, 0);
        _count.CopyTo(newCount, 0);
        _stack.CopyTo(newStack, 0);

        _items = newItems;
        _taken = newTaken;
        _count = newCount;
        _stack = newStack;
        _size = newSize;
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

    private static class Thrower
    {
        public static void ThrowOnGet(uint index) => throw new ArgumentException($"Attempt to get not persistent item by index {index}");
        public static void ThrowOnVersion(uint version) => throw new ArgumentException($"Attempt to get not persistent item by version {version}");
    }
}

public readonly struct VersId<T>
{
    public readonly uint id;
    public readonly uint version;

    internal VersId(uint id, uint version)
    {
        this.id = id;
        this.version = version;
    }
}