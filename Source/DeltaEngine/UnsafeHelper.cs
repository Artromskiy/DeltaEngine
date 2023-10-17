using System;
using System.Buffers;

namespace DeltaEngine
{
    internal static class UnsafeHelper
    {

    }

    public readonly unsafe ref struct pin<T> where T : struct
    {
        private static int refCount;
        private readonly MemoryHandle _handle;
        public T* handle => (T*)_handle.Pointer;
        public pin(T[] val)
        {
            fixed (void* h = val)
                _handle = new MemoryHandle(h);
            refCount++;
        }
        public pin(Span<T> val)
        {
            fixed (void* h = val)
                _handle = new MemoryHandle(h);
            refCount++;
        }
        public void Dispose()
        {
            _handle.Dispose();
            refCount--;
        }
    }
}
