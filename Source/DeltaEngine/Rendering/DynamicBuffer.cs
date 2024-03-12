using Silk.NET.Vulkan;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;


namespace Delta.Rendering;
internal class DynamicBuffer
{
    private Buffer _buffer;
    private DeviceMemory _memory;
    private readonly RenderBase _renderBase;

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
        _cmdBuffer = RenderHelper.CreateCommandBuffer(_renderBase, _renderBase.deviceQ.transferCmdPool);
    }

    public Buffer GetBuffer() => _buffer;

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

        _ = _renderBase.vk.CreateBuffer(_renderBase.deviceQ.device, createInfo, null, out buffer);
        var reqs = _renderBase.vk.GetBufferMemoryRequirements(_renderBase.deviceQ.device, buffer);

        // Recreate buffer with full size given by requirements
        _renderBase.vk.DestroyBuffer(_renderBase.deviceQ.device, buffer, null);
        createInfo.Size = reqs.Size;
        _ = _renderBase.vk.CreateBuffer(_renderBase.deviceQ.device, createInfo, null, out buffer);

        var memProps = MemoryPropertyFlags.DeviceLocalBit;
        uint memType = RenderHelper.FindMemoryType(_renderBase, (int)reqs.MemoryTypeBits, memProps);
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = memType
        };
        size = reqs.Size;
        _ = _renderBase.vk.AllocateMemory(_renderBase.deviceQ.device, allocateInfo, null, out memory);
        _ = _renderBase.vk.BindBufferMemory(_renderBase.deviceQ.device, buffer, memory, 0);
    }

    [MethodImpl(Inl)]
    public void UpdateFrom<T>(GpuArray<T> array) where T : unmanaged
    {
        var sourceSize = array.Size;
        if (sourceSize > _size)
            Resize(sourceSize);
        CopyBuffer(array.GetBuffer(), sourceSize, _buffer, sourceSize);
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
        _renderBase.vk.DestroyBuffer(_renderBase.deviceQ.device, _buffer, null);
        _renderBase.vk.FreeMemory(_renderBase.deviceQ.device, _memory, null);
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
        if (res != Result.Success)
            Console.WriteLine("here");
        if (res == Result.Success)
            _ = vk.WaitForFences(_renderBase.deviceQ.device, 1, &fence, true, ulong.MaxValue);

        vk.ResetFences(_renderBase.deviceQ.device, 1, &fence);
        vk.ResetCommandBuffer(cmdBuffer, 0);
    }
}