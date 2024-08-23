using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Delta.Rendering.Collections;
internal class DynamicBuffer
{
    protected readonly RenderBase _renderBase;

    private Buffer _buffer;
    private DeviceMemory _memory;

    private Fence _copyFence;
    private CommandBuffer _cmdBuffer;

    public bool ChangedBuffer { get; set; } = true;

    private ulong _size;
    public ulong Size => _size;

    public DynamicBuffer(RenderBase renderBase, ulong sizeBytes)
    {
        _renderBase = renderBase;
        _size = sizeBytes;
        CreateBuffer(ref _size, out _buffer, out _memory);
        _copyFence = RenderHelper.CreateFence(_renderBase, false);
        _cmdBuffer = _renderBase.CreateCommandBuffer(_renderBase.deviceQ.transferCmdPool);
    }

    public Buffer GetBuffer() => _buffer;
    public static implicit operator Buffer(DynamicBuffer buffer) => buffer._buffer;

    private unsafe void CreateBuffer(ref ulong size, out Buffer buffer, out DeviceMemory memory)
    {
        var usage = BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit;
        BufferCreateInfo createInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            Flags = default,
        };

        _ = _renderBase.vk.CreateBuffer(_renderBase.deviceQ, createInfo, null, out buffer);
        var reqs = _renderBase.vk.GetBufferMemoryRequirements(_renderBase.deviceQ, buffer);

        // Recreate buffer with full size given by requirements
        _renderBase.vk.DestroyBuffer(_renderBase.deviceQ, buffer, null);
        createInfo.Size = reqs.Size;
        _ = _renderBase.vk.CreateBuffer(_renderBase.deviceQ, createInfo, null, out buffer);

        var memProps = MemoryPropertyFlags.DeviceLocalBit;
        uint memType = RenderHelper.FindMemoryType(_renderBase, (int)reqs.MemoryTypeBits, memProps);
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = memType
        };
        size = reqs.Size;
        _ = _renderBase.vk.AllocateMemory(_renderBase.deviceQ, allocateInfo, null, out memory);
        _ = _renderBase.vk.BindBufferMemory(_renderBase.deviceQ, buffer, memory, 0);
    }

    [MethodImpl(Inl)]
    public void UpdateFrom<T>(GpuArray<T> array) where T : unmanaged
    {
        var sourceSize = array.Size;
        if (sourceSize > _size)
            Resize(sourceSize);
        CopyBuffer(array.Buffer, sourceSize, _buffer, sourceSize);
    }

    public unsafe void EnsureSize(ulong size)
    {
        ulong newSize = BitOperations.RoundUpToPowerOf2(size);
        if (_size < newSize)
            Resize(newSize);
    }

    private unsafe void Resize(ulong size)
    {
        ulong newSize = BitOperations.RoundUpToPowerOf2(size);
        CreateBuffer(ref newSize, out var newBuffer, out var newMemory);
        _renderBase.vk.DestroyBuffer(_renderBase.deviceQ, _buffer, null);
        _renderBase.vk.FreeMemory(_renderBase.deviceQ, _memory, null);
        _memory = newMemory;
        _buffer = newBuffer;
        _size = newSize;
        ChangedBuffer = true;
    }

    [MethodImpl(Inl)]
    private unsafe void CopyBuffer(Buffer source, ulong sourceSize, Buffer destionation, ulong destinationSize)
    {
        Vk vk = _renderBase.vk;

        CommandBuffer cmdBuffer = _cmdBuffer;
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _ = vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
        BufferCopy copy = new(0, 0, Math.Min(sourceSize, destinationSize));
        vk.CmdCopyBuffer(cmdBuffer, source, destionation, 1, &copy);
        _ = vk.EndCommandBuffer(cmdBuffer);
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer
        };
        var fence = _copyFence;
        var res = vk.QueueSubmit(_renderBase.deviceQ.graphicsQueue, 1, &submitInfo, fence);
        _ = res;
        if (res == Result.Success)
            _ = vk.WaitForFences(_renderBase.deviceQ, 1, &fence, true, ulong.MaxValue);

        vk.ResetFences(_renderBase.deviceQ, 1, &fence);
        vk.ResetCommandBuffer(cmdBuffer, 0);
    }
}