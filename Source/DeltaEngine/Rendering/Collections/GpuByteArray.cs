using Delta.Rendering.Internal;
using Delta.Runtime;
using Delta.Utilities;
using Silk.NET.Vulkan;
using System;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Delta.Rendering.Collections;
public unsafe class GpuByteArray : IDisposable
{
    private readonly DeviceQueues _deviceQ;
    private readonly Vk _vk;

    private nint _pData;

    private Buffer _buffer;
    private DeviceMemory _memory;

    private Fence _fence;
    private CommandBuffer _cmdBuffer;

    internal Buffer Buffer => _buffer;

    private int _size;
    public int Size => _size;
    protected nint PData => _pData;


    internal unsafe GpuByteArray(Vk vk, DeviceQueues deviceQ, int size)
    {
        _vk = vk;
        _deviceQ = deviceQ;

        CreateBuffer(ref size, out _buffer, out _memory, out _pData);
        _fence = RenderHelper.CreateFence(_vk, _deviceQ, false);
        _cmdBuffer = RenderHelper.CreateCommandBuffer(_vk, _deviceQ, _deviceQ.GetCmdPool(QueueType.Transfer));
        _size = size;
    }

    public GpuByteArray(int size)
    {
        _vk = IRuntimeContext.Current.GraphicsModule.RenderData.vk;
        _deviceQ = IRuntimeContext.Current.GraphicsModule.RenderData.deviceQ;

        CreateBuffer(ref size, out _buffer, out _memory, out _pData);
        _fence = RenderHelper.CreateFence(_vk, _deviceQ, false);
        _cmdBuffer = RenderHelper.CreateCommandBuffer(_vk, _deviceQ, _deviceQ.GetCmdPool(QueueType.Transfer));
        _size = size;
    }

    [Imp(Inl)]
    public virtual void Resize(int size)
    {
        int newSize = size;

        CreateBuffer(ref newSize, out var newBuffer, out var newMemory, out var newPtr);
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        var cmdBuffer = _cmdBuffer;
        _vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
        _vk.CmdCopyBuffer(cmdBuffer, _buffer, newBuffer, 1, new BufferCopy(0, 0, (ulong)Math.Min(_size, newSize)));
        _vk.EndCommandBuffer(cmdBuffer);
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer
        };
        _ = _vk.QueueSubmit(_deviceQ.GetQueue(QueueType.Transfer), 1, &submitInfo, _fence);
        _ = _vk.WaitForFences(_deviceQ, 1, _fence, true, ulong.MaxValue);
        _ = _vk.ResetCommandBuffer(cmdBuffer, 0);
        _ = _vk.ResetFences(_deviceQ, 1, _fence);
        _vk.DestroyBuffer(_deviceQ, _buffer, null);
        _vk.UnmapMemory(_deviceQ, _memory);
        _vk.FreeMemory(_deviceQ, _memory, null);
        _memory = newMemory;
        _buffer = newBuffer;
        _size = newSize;
        _pData = newPtr;
    }

    private void CreateBuffer(ref int size, out Buffer buffer, out DeviceMemory memory, out nint data)
    {
        var usage = BufferUsageFlags.TransferDstBit | BufferUsageFlags.TransferSrcBit;
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
        var memProps = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit;
        uint memType = _deviceQ.gpu.FindMemoryType(reqs.MemoryTypeBits, memProps, out var memPropsFound);
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = memType
        };
        size = (int)reqs.Size;
        _ = _vk.AllocateMemory(_deviceQ, allocateInfo, null, out memory);
        createInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = (ulong)size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            Flags = default,
        };
        _vk.DestroyBuffer(_deviceQ, buffer, null);
        _ = _vk.CreateBuffer(_deviceQ, createInfo, null, out buffer);
        _ = _vk.BindBufferMemory(_deviceQ, buffer, memory, 0);

        void* pdata = default;

        _vk.MapMemory(_deviceQ, memory, 0, reqs.Size, 0, &pdata);

        data = new(pdata);
    }

    public void Dispose()
    {
        _vk.DestroyBuffer(_deviceQ, _buffer, null);
        _vk.UnmapMemory(_deviceQ, _memory);
        _vk.FreeMemory(_deviceQ, _memory, null);
    }
}
