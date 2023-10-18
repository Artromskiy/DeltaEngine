#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
#pragma warning disable IDE1006 // Naming Styles
using System;
using System.Buffers;

namespace DeltaEngine;

internal readonly unsafe ref struct pin<T> where T : struct
{
    private static int refCount;
    private readonly MemoryHandle _handle;
    internal T* handle => (T*)_handle.Pointer;
    internal pin(T[] val)
    {
        fixed (void* h = val)
            _handle = new MemoryHandle(h);
        refCount++;
    }
    internal pin(Span<T> val)
    {
        fixed (void* h = val)
            _handle = new MemoryHandle(h);
        refCount++;
    }
    internal void Dispose()
    {
        _handle.Dispose();
        refCount--;
    }
}
