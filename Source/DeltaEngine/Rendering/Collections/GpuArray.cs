using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using System;
using System.Numerics;

namespace Delta.Rendering.Collections;
public unsafe class GpuArray<T> : GpuByteArray where T : unmanaged
{
    private int _length;
    public int Length => _length;

    private PointerWriter<T> _writer;
    public PointerWriter<T> Writer => _writer;

    internal unsafe GpuArray(Vk vk, DeviceQueues deviceQ, uint length) : base(vk, deviceQ, 1)
    {
        _length = (int)Math.Max(1, BitOperations.RoundUpToPowerOf2(length + 1));
        int size = _length * sizeof(T);
        Resize(size);
        _writer = new PointerWriter<T>(PData, _length);
    }

    public GpuArray(uint length) : base(1)
    {
        _length = (int)Math.Max(1, BitOperations.RoundUpToPowerOf2(length + 1));
        int size = _length * sizeof(T);
        Resize(size);
        _writer = new PointerWriter<T>(PData, _length);
    }

    [Imp(Inl)]
    public override void Resize(int length)
    {
        _length = length;
        int newSize = length * sizeof(T);
        base.Resize(newSize);
        _writer = new PointerWriter<T>(PData, _length);
    }
}