using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Delta.Utilities;
internal readonly ref struct JaggedSpan<T>
{
    private readonly ref T[] _reference;
    private readonly int _length;
    public int Length => _length;
    public JaggedSpan(T[][] jaggedArray)
    {
        _reference = ref MemoryMarshal.GetArrayDataReference(jaggedArray);
        _length = jaggedArray.Length;
    }

    public readonly ReadOnlySpan<T> this[int index] => new(Unsafe.Add(ref _reference, (nint)(uint)index));

    public static implicit operator JaggedSpan<T>(T[][] jaggedArray) => new(jaggedArray);
}
