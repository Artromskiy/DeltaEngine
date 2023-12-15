using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace DeltaEngine.Rendering;
internal unsafe class StorageDynamicArray<T> : IDisposable where T : unmanaged
{
    private nint _pData;
    private Buffer _buffer;
    private DeviceMemory _memory;

    private Fence _fence;
    private CommandBuffer _cmdBuffer;

    private uint _length;
    private ulong _size;

    protected uint Length => _length;
    public ulong Size => _size;

    private readonly RenderBase _renderBase;

    public unsafe StorageDynamicArray(RenderBase renderBase, uint length)
    {
        _renderBase = renderBase;
        _length = length;
        ulong size = (ulong)(sizeof(T) * length);
        CreateBuffer(ref size, out _buffer, out _memory, out _pData);
        _fence = RenderHelper.CreateFence(_renderBase, false);
        _cmdBuffer = RenderHelper.CreateCommandBuffer(_renderBase, _renderBase.deviceQueues.transferCmdPool);
        _size = size;
    }

    protected T this[uint index]
    {
        [MethodImpl(Inl)]
        set
        {
            Debug.Assert(index >= 0 && index < _length);
            ref var destination = ref Unsafe.Add(ref Unsafe.AsRef<T>(_pData.ToPointer()), index);
            Unsafe.WriteUnaligned(Unsafe.AsPointer(ref destination), value);
        }
    }
    internal Buffer GetBuffer() => _buffer;
    protected Writer GetWriter() => new(_length, _pData);

    /// <summary>
    /// Fastest way to write directly to gpu memory
    /// </summary>
    /// <param name="length"></param>
    /// <param name="pData"></param>
    protected readonly struct Writer(uint length, nint pData)
    {
        private readonly uint _length = length;
        private readonly nint _pData = pData;
        private static readonly uint sizeOf = (uint)sizeof(T);

        public readonly ref T this[uint index]
        {
            [MethodImpl(Inl)]
            get
            {
                Debug.Assert(index >= 0 && index < _length);
                nint destination = _pData + (nint)(index * sizeOf);
                return ref Unsafe.AsRef<T>(destination.ToPointer());
            }
        }
    }

    [MethodImpl(Inl)]
    protected void Resize(uint length)
    {
        _length = length;
        ulong newSize = (ulong)(sizeof(T) * length);

        CreateBuffer(ref newSize, out var newBuffer, out var newMemory, out var newPtr);
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        var cmdBuffer = _cmdBuffer;
        _renderBase.vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
        var copy = new BufferCopy(0, 0, Math.Min(_size, newSize));
        _renderBase.vk.CmdCopyBuffer(cmdBuffer, _buffer, newBuffer, 1, &copy);
        _renderBase.vk.EndCommandBuffer(cmdBuffer);
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer
        };
        _ = _renderBase.vk.QueueSubmit(_renderBase.deviceQueues.transferQueue, 1, &submitInfo, _fence);
        _ = _renderBase.vk.WaitForFences(_renderBase.deviceQueues.device, 1, _fence, true, ulong.MaxValue);
        _ = _renderBase.vk.ResetCommandBuffer(cmdBuffer, 0);
        _ = _renderBase.vk.ResetFences(_renderBase.deviceQueues.device, 1, _fence);
        _renderBase.vk.DestroyBuffer(_renderBase.deviceQueues.device, _buffer, null);
        _renderBase.vk.UnmapMemory(_renderBase.deviceQueues.device, _memory);
        _renderBase.vk.FreeMemory(_renderBase.deviceQueues.device, _memory, null);
        _memory = newMemory;
        _buffer = newBuffer;
        _size = newSize;
        _pData = newPtr;
    }

    private void CreateBuffer(ref ulong size, out Buffer buffer, out DeviceMemory memory, out nint data)
    {
        var usage = BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit | BufferUsageFlags.TransferSrcBit;
        BufferCreateInfo createInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            Flags = default,
        };
        _ = _renderBase.vk.CreateBuffer(_renderBase.deviceQueues.device, createInfo, null, out buffer);
        var reqs = _renderBase.vk.GetBufferMemoryRequirements(_renderBase.deviceQueues.device, buffer);
        var memProps = MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit;
        uint memType = RenderHelper.FindMemoryType(_renderBase, (int)reqs.MemoryTypeBits, memProps);
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = memType
        };
        size = reqs.Size;
        _ = _renderBase.vk.AllocateMemory(_renderBase.deviceQueues.device, allocateInfo, null, out memory);
        _ = _renderBase.vk.BindBufferMemory(_renderBase.deviceQueues.device, buffer, memory, 0);

        void* pdata = default;

        _renderBase.vk.MapMemory(_renderBase.deviceQueues.device, memory, 0, reqs.Size, 0, &pdata);

        data = new(pdata);
    }

    public void Dispose()
    {
        _renderBase.vk.DestroyBuffer(_renderBase.deviceQueues.device, _buffer, null);
        _renderBase.vk.UnmapMemory(_renderBase.deviceQueues.device, _memory);
        _renderBase.vk.FreeMemory(_renderBase.deviceQueues.device, _memory, null);
    }
}