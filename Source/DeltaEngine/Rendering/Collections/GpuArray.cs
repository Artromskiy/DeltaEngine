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

    internal unsafe GpuArray(Vk vk, DeviceQueues deviceQ, int length) : base(vk, deviceQ, 1)
    {
        Resize(length);
        _writer = new PointerWriter<T>(PData, _length);
    }

    public GpuArray(int length) : base(1)
    {
        Resize(length);
        _writer = new PointerWriter<T>(PData, _length);
    }

    [Imp(Inl)]
    public override void Resize(int length)
    {
        _length = (int)Math.Max(1, BitOperations.RoundUpToPowerOf2(checked((uint)(length))));
        int newSize = _length * sizeof(T);
        base.Resize(newSize);
        _writer = new PointerWriter<T>(PData, _length);
    }
}