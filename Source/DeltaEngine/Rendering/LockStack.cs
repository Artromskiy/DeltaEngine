using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Delta.Rendering;
internal class LockStack<T>
{
    private T[] _values = [];
    private int _count;

    public int Count => _count;

    public void EnsureCapacity(int capacity)
    {
        if (_values.Length < capacity)
            Array.Resize(ref _values, (int)BitOperations.RoundUpToPowerOf2((uint)capacity));
    }

    public void Push(T value)
    {
        int index = Interlocked.Increment(ref _count) - 1;
        _values[index - 1] = value;
    }

    public ref T Pop()
    {
        int index = Interlocked.Decrement(ref _count) + 1;
        if (index >= 0)
            return ref _values[index];
        else
            return ref Unsafe.NullRef<T>();
    }

    public Span<T> AsSpan() => new(_values, 0, _count);
}
