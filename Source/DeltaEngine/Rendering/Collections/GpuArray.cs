using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Delta.Rendering.Collections;
internal unsafe class GpuArray<T> : IDisposable where T : unmanaged
{
    private nint _pData;
    private Buffer _buffer;
    private DeviceMemory _memory;

    private Fence _fence;
    private CommandBuffer _cmdBuffer;

    private bool _needsToFlush;

    private uint _length;
    private ulong _size;

    public uint Length => _length;
    public ulong Size => _size;

    private readonly DeviceQueues _deviceQ;
    private readonly Vk _vk;
    //private readonly RenderBase _renderBase;

    public unsafe GpuArray(Vk vk, DeviceQueues deviceQ, uint length)
    {
        _vk = vk;
        _deviceQ = deviceQ;
        _length = Math.Max(1, BitOperations.RoundUpToPowerOf2(length + 1));

        ulong size = (ulong)(sizeof(T) * _length);
        CreateBuffer(ref size, out _buffer, out _memory, out _pData);
        _fence = RenderHelper.CreateFence(_vk, _deviceQ, false);
        _cmdBuffer = RenderHelper.CreateCommandBuffer(_vk, _deviceQ, _deviceQ.GetCmdPool(QueueType.Transfer));
        _size = size;
    }

    protected T this[uint index]
    {
        [Imp(Inl)]
        set
        {
            _ = index >= 0 && index < _length;
            ref var destination = ref Unsafe.Add(ref Unsafe.AsRef<T>(_pData.ToPointer()), index);
            Unsafe.WriteUnaligned(Unsafe.AsPointer(ref destination), value);
        }
    }
    internal Buffer Buffer => _buffer;
    public GpuWriter Writer => new(_length, _pData);
    public Span<T> Span => new(_pData.ToPointer(), (int)_length);

    /// <summary>
    /// Fastest way to write directly to gpu memory
    /// </summary>
    /// <param name="length"></param>
    /// <param name="pData"></param>
    public readonly struct GpuWriter(uint length, nint pData)
    {
        private readonly uint _length = length;
        private readonly nint _pData = pData;

        public readonly ref T this[uint index]
        {
            [Imp(Inl)]
            get
            {
                Debug.Assert(index >= 0 && index < _length);
                return ref Unsafe.Add(ref Unsafe.AsRef<T>((void*)_pData), index);
            }
        }
        public ReadOnlySpan<T> Data => new(_pData.ToPointer(), (int)_length);
    }

    public void Flush(uint min, uint max) // TODO use these ranges according to vulkan docs about MappedMemoryRange and "multiple of n bytes per fucking transfer"
    {
        if (!_needsToFlush)
            return;
        var memRng = new MappedMemoryRange()
        {
            SType = StructureType.MappedMemoryRange,
            Memory = _memory,
            Size = _size
        };
        _vk.FlushMappedMemoryRanges(_deviceQ, 1, memRng);
    }

    [Imp(Inl)]
    public void Resize(uint length)
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
        _vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
        _vk.CmdCopyBuffer(cmdBuffer, _buffer, newBuffer, 1, new BufferCopy(0, 0, Math.Min(_size, newSize)));
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

    private void CreateBuffer(ref ulong size, out Buffer buffer, out DeviceMemory memory, out nint data)
    {
        var usage = BufferUsageFlags.TransferDstBit | BufferUsageFlags.TransferSrcBit;
        BufferCreateInfo createInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            Flags = default,
        };
        _ = _vk.CreateBuffer(_deviceQ, createInfo, null, out buffer);
        var reqs = _vk.GetBufferMemoryRequirements(_deviceQ, buffer);
        var memProps = MemoryPropertyFlags.HostVisibleBit;
        uint memType = _deviceQ.gpu.FindMemoryType(reqs.MemoryTypeBits, memProps, out var memPropsFound);
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = memType
        };
        size = reqs.Size;
        _ = _vk.AllocateMemory(_deviceQ, allocateInfo, null, out memory);
        createInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive,
            Flags = default,
        };
        _needsToFlush = !memPropsFound.HasFlag(MemoryPropertyFlags.HostCoherentBit);
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