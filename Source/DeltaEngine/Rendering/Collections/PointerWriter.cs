using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Delta.Rendering.Collections;

/// <summary>
/// Fastest way to write directly to gpu memory by pointer
/// </summary>
/// <param name="length"></param>
/// <param name="pData"></param>
public readonly unsafe struct PointerWriter<T>(nint pData, int length)
{
    private readonly nint _pData = pData;
    private readonly int _length = length;

    public readonly ref T this[int index]
    {
        [Imp(Inl)]
        get
        {
            Debug.Assert(index >= 0 && index < _length);
            return ref Unsafe.Add(ref Unsafe.AsRef<T>(_pData.ToPointer()), index);
        }
    }
    public ReadOnlySpan<T> Data => new(_pData.ToPointer(), _length);
}
