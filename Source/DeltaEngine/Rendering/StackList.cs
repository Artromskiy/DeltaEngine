using System;
using System.Runtime.CompilerServices;

namespace DeltaEngine.Rendering;

public class StackList<T>
{
    internal T[] _items;
    internal int _lastFree;

    private int[] _stack;
    private int _stackSize;

    private bool[] _taken;

    public StackList()
    {
        _items = Array.Empty<T>();
        _stack = Array.Empty<int>();
        _taken = Array.Empty<bool>();
    }

    public int Add(T item)
    {
        int index;
        if (_stackSize > 0)
            index = PopStack();
        else
        {
            if (_lastFree == _items.Length)
                Grow();
            index = _lastFree++;
        }
        _taken[index] = true;
        _items[index] = item;
        return index;
    }

    public int Next() => _stackSize > 0 ? PeekStack() : _lastFree;

    internal ref T this[int index]
    {
        get
        {
            CheckIndex(index);
            return ref _items[index];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Update(int index, T item)
    {
        CheckIndex(index);
        _items[index] = item;
    }

    public T Get(int index)
    {
        CheckIndex(index);
        return _items[index];
    }

    public void RemoveAt(int index)
    {
        CheckIndex(index);
        _items[index] = default!;
        _taken[index] = false;
        PushStack(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckIndex(int index)
    {
        if (index < 0 || index >= _lastFree || !_taken[index])
            Thrower.ThrowOnGet(index);
    }

    public bool Has(int index) => index >= 0 && index < _lastFree && _taken[index];

    private void Grow()
    {
        int newSize = _lastFree == 0 ? 1 : _lastFree * 2;

        T[] newItems = new T[newSize];
        int[] newStack = new int[newSize];
        bool[] newTaken = new bool[newSize];

        _items.CopyTo(newItems, 0);
        _stack.CopyTo(newStack, 0);
        _taken.CopyTo(newTaken, 0);

        _items = newItems;
        _stack = newStack;
        _taken = newTaken;
    }

    private int PopStack()
    {
        Debug.Assert(_stackSize > 0);
        return _stack[--_stackSize];
    }

    private int PeekStack()
    {
        Debug.Assert(_stackSize > 0);
        return _stack[_stackSize - 1];
    }

    private void PushStack(int index)
    {
        Debug.Assert((uint)_stackSize < (uint)_stack.Length);
        _stack[_stackSize++] = index;
    }

    private static class Thrower
    {
        public static void ThrowOnGet(int index) => throw new ArgumentException($"Attempt to get not persistent item by index {index}");
    }
}