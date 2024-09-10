using Delta.Rendering.Internal;
using Delta.Utilities;
using Silk.NET.Vulkan;
using System;
using System.Collections.Immutable;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Delta.Rendering.Headless;
internal class SwapChain : IDisposable
{
    public readonly ImmutableArray<DeviceMemory> imagesMemory;
    public readonly ImmutableArray<Image> images;
    public readonly ImmutableArray<ImageView> imageViews;
    public readonly ImmutableArray<Framebuffer> frameBuffers;

    private readonly Buffer _hostBuffer;
    private readonly DeviceMemory _hostBufferMemory;
    private UnmanagedMemoryManager<byte>? _renderMemoryManager;
    public Memory<byte> RenderStream => _renderMemoryManager!.Memory;

    private Fence _copyFence;
    private CommandBuffer _cmdBuffer;

    private int _currentFrameIndex = -1;


    public int imageCount;
    public readonly int width;
    public readonly int height;

    public Extent2D Extent => new((uint)width, (uint)height);

    private readonly RenderBase data;

    public unsafe SwapChain(RenderBase data, uint trgImageCount, Format format, int width, int height)
    {
        this.data = data;
        this.width = width;
        this.height = height;
        var size = width * height * 4;

        uint maxImageCount = int.MaxValue;
        uint minImageCount = 1;

        imageCount = (int)Math.Clamp(trgImageCount, minImageCount, maxImageCount);

        _copyFence = RenderHelper.CreateFence(data.vk, data.deviceQ, false);

        (images, imagesMemory) = RenderHelper.CreateImages(
            data.vk, data.deviceQ, imageCount, width, height, format,
            ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.DeviceLocalBit,
            ImageTiling.Optimal);

        imageViews = RenderHelper.CreateImageViews(data.vk, data.deviceQ, [.. images], format);
        frameBuffers = RenderHelper.CreateFramebuffers(data.vk, data.deviceQ, [.. imageViews], data.renderPass, width, height);

        _cmdBuffer = RenderHelper.CreateCommandBuffer(data.vk, data.deviceQ, data.deviceQ.GetCmdPool(QueueType.Transfer));

        (_hostBuffer, _hostBufferMemory) = RenderHelper.CreateBufferAndMemory(
            data.vk, data.deviceQ, (uint)size,
            BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.HostVisibleBit);

        void* ptr = default;
        _ = data.vk.MapMemory(data.deviceQ, _hostBufferMemory, 0, Vk.WholeSize, 0, &ptr);
        _renderMemoryManager = new(new nint(ptr), size);
    }


    public void Present(Semaphore waitSemaphore)
    {
        RenderHelper.CopyImage(data.vk, _cmdBuffer, data.deviceQ,
            images[_currentFrameIndex], _hostBuffer, width, height,
            waitSemaphore, _copyFence);

        data.vk.WaitForFences(data.deviceQ, 1, _copyFence, true, ulong.MaxValue);
        data.vk.ResetFences(data.deviceQ, 1, _copyFence);
    }


    public int AcquireNextImage()
    {
        _currentFrameIndex++;
        _currentFrameIndex %= imageCount;
        return _currentFrameIndex;
    }

    public unsafe void Dispose()
    {
        data.vk.DeviceWaitIdle(data.deviceQ);

        foreach (var framebuffer in frameBuffers)
            data.vk.DestroyFramebuffer(data.deviceQ, framebuffer, null);
        foreach (var imageView in imageViews)
            data.vk.DestroyImageView(data.deviceQ, imageView, null);
        foreach (var image in images)
            data.vk.DestroyImage(data.deviceQ, image, null);
        foreach (var memory in imagesMemory)
            data.vk.FreeMemory(data.deviceQ, memory, null);
        data.vk.FreeCommandBuffers(data.deviceQ, data.deviceQ.GetCmdPool(QueueType.Transfer), 1, _cmdBuffer);

        _renderMemoryManager = null;
    }
}
