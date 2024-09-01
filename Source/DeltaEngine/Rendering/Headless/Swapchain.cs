using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using System;
using System.Collections.Immutable;

namespace Delta.Rendering.Headless;
internal class SwapChain : IDisposable
{
    public readonly ImmutableArray<DeviceMemory> imagesMemory;
    public readonly ImmutableArray<Image> images;
    public readonly ImmutableArray<ImageView> imageViews;
    public readonly ImmutableArray<Framebuffer> frameBuffers;

    private readonly Image _hostImage;
    private readonly DeviceMemory _hostMemory;

    private CommandBuffer _cmdBuffer;

    public readonly SurfaceFormatKHR format;
    public Extent2D extent;

    private int _currentFrameIndex = -1;

    public int imageCount;

    private readonly RenderBase data;

    public unsafe SwapChain(RenderBase data, uint trgImageCount, Format format)
    {
        this.data = data;

        uint w = 0, h = 0;
        extent = new(w, h);

        uint maxImageCount = 0;
        uint minImageCount = 1;

        maxImageCount = maxImageCount == 0 ? int.MaxValue : maxImageCount;
        imageCount = (int)Math.Clamp(trgImageCount, minImageCount, maxImageCount);


        images = RenderHelper.CreateImages(data.vk, data.deviceQ, imageCount, 1000, 1000, format, out imagesMemory);
        imageViews = RenderHelper.CreateImageViews(data.vk, data.deviceQ, [.. images], format);
        frameBuffers = RenderHelper.CreateFramebuffers(data.vk, data.deviceQ, [.. imageViews], data.renderPass, extent);

        _cmdBuffer = RenderHelper.CreateCommandBuffer(data.vk, data.deviceQ, data.deviceQ.GetCmdPool(QueueType.Transfer));
        (_hostImage, _hostMemory) = RenderHelper.CreateImage(data.vk, data.deviceQ, w, h, format,
            ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit);
    }

    public void Present(Semaphore waitSemaphore, Semaphore signalSemaphore)
    {
        RenderHelper.CopyImage(data.vk, _cmdBuffer, data.deviceQ,
            images[_currentFrameIndex], _hostImage, (int)extent.Width, (int)extent.Height,
            waitSemaphore, signalSemaphore);
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
    }
}
