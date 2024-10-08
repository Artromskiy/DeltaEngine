﻿using Delta.Rendering.Internal;
using Delta.Runtime;
using Silk.NET.Vulkan;
using System;
using System.Numerics;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Delta.Rendering.Collections;

internal class DynamicBuffer
{
    protected readonly Vk _vk;
    protected readonly DeviceQueues _deviceQ;

    private Buffer _buffer;
    private DeviceMemory _memory;

    private Fence _copyFence;
    private CommandBuffer _cmdBuffer;

    public bool ChangedBuffer { get; protected set; } = true;

    private int _size;
    public int Size => _size;

    public DynamicBuffer(Vk vk, DeviceQueues deviceQ, int sizeBytes)
    {
        _vk = vk;
        _deviceQ = deviceQ;
        _size = sizeBytes;
        CreateBuffer(ref _size, out _buffer, out _memory);
        _copyFence = RenderHelper.CreateFence(_vk, _deviceQ, false);
        _cmdBuffer = RenderHelper.CreateCommandBuffer(_vk, _deviceQ, _deviceQ.GetCmdPool(QueueType.Transfer));
    }

    public DynamicBuffer(int sizeBytes)
    {
        var data = IRuntimeContext.Current.GraphicsModule.RenderData;
        _vk = data.vk;
        _deviceQ = data.deviceQ;
        _size = sizeBytes;
        CreateBuffer(ref _size, out _buffer, out _memory);
        _copyFence = RenderHelper.CreateFence(_vk, _deviceQ, false);
        _cmdBuffer = RenderHelper.CreateCommandBuffer(_vk, _deviceQ, _deviceQ.GetCmdPool(QueueType.Transfer));
    }

    public Buffer GetBuffer() => _buffer;
    public static implicit operator Buffer(DynamicBuffer buffer) => buffer._buffer;

    private unsafe void CreateBuffer(ref int size, out Buffer buffer, out DeviceMemory memory)
    {
        var usage = BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit;
        BufferCreateInfo createInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            Flags = default,
        };

        _ = _vk.CreateBuffer(_deviceQ, createInfo, null, out buffer);
        var reqs = _vk.GetBufferMemoryRequirements(_deviceQ, buffer);

        // Recreate buffer with full size given by requirements
        _vk.DestroyBuffer(_deviceQ, buffer, null);
        createInfo.Size = reqs.Size;
        _ = _vk.CreateBuffer(_deviceQ, createInfo, null, out buffer);

        var memProps = MemoryPropertyFlags.DeviceLocalBit;
        uint memType = _deviceQ.gpu.FindMemoryType(reqs.MemoryTypeBits, memProps);
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = memType
        };
        size = (int)reqs.Size;
        _ = _vk.AllocateMemory(_deviceQ, allocateInfo, null, out memory);
        _ = _vk.BindBufferMemory(_deviceQ, buffer, memory, 0);
    }

    [Imp(Inl)]
    public void UpdateFrom<T>(GpuArray<T> array) where T : unmanaged
    {
        var sourceSize = array.Size;
        if (sourceSize > _size)
            Resize(sourceSize);
        CopyBuffer(array.Buffer, sourceSize, _buffer, sourceSize);
    }

    [Imp(Inl)]
    public void UpdateFrom(GpuByteArray array)
    {
        var sourceSize = array.Size;
        if (sourceSize > _size)
            Resize(sourceSize);
        CopyBuffer(array.Buffer, sourceSize, _buffer, sourceSize);
    }

    public unsafe void EnsureSize(int size)
    {
        int newSize = (int)BitOperations.RoundUpToPowerOf2((uint)size);
        if (_size < newSize)
            Resize(newSize);
    }

    private unsafe void Resize(int size)
    {
        int newSize = (int)BitOperations.RoundUpToPowerOf2((uint)size);
        CreateBuffer(ref newSize, out var newBuffer, out var newMemory);
        _vk.DestroyBuffer(_deviceQ, _buffer, null);
        _vk.FreeMemory(_deviceQ, _memory, null);
        _memory = newMemory;
        _buffer = newBuffer;
        _size = newSize;
        ChangedBuffer = true;
    }

    [Imp(Inl)]
    private unsafe void CopyBuffer(Buffer source, int sourceSize, Buffer destionation, int destinationSize)
    {
        CommandBuffer cmdBuffer = _cmdBuffer;
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _ = _vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
        BufferCopy copy = new(0, 0, (ulong)Math.Min(sourceSize, destinationSize));
        _vk.CmdCopyBuffer(cmdBuffer, source, destionation, 1, &copy);
        _ = _vk.EndCommandBuffer(cmdBuffer);
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer
        };
        var fence = _copyFence;
        var res = _vk.QueueSubmit(_deviceQ.GetQueue(QueueType.Graphics), 1, &submitInfo, fence);
        _ = res;
        if (res == Result.Success)
            _ = _vk.WaitForFences(_deviceQ, 1, &fence, true, ulong.MaxValue);

        _vk.ResetFences(_deviceQ, 1, &fence);
        _vk.ResetCommandBuffer(cmdBuffer, 0);
    }
}