using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace DeltaEngine.Rendering;
internal class StaggingBuffer : IDisposable
{
    public Buffer buffer;
    public DeviceMemory memory;

    private readonly RenderBase _renderBase;

    public unsafe StaggingBuffer(RenderBase data, ref byte bufferData, int bufferSize)
    {
        _renderBase = data;
        (buffer, memory) = RenderHelper.CreateBuffer(_renderBase, (ulong)bufferSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        void* datap;
        _renderBase.vk.MapMemory(_renderBase.device, memory, 0, (ulong)bufferSize, 0, &datap);
        Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(datap),
            ref bufferData,
            (uint)bufferSize);
        _renderBase.vk.UnmapMemory(_renderBase.device, memory);
    }
    public unsafe void Dispose()
    {
        _renderBase.vk.DestroyBuffer(_renderBase.device, buffer, null);
        _renderBase.vk.FreeMemory(_renderBase.device, memory, null);
    }
}
