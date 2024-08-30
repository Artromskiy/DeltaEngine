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
    public readonly SurfaceFormatKHR format;
    public Extent2D extent;

    public int imageCount;

    private readonly RenderBase data;

    public unsafe SwapChain(RenderBase data, uint trgImageCount, Format format)
    {
        this.data = data;

        uint w = 0, h = 0;
        extent = new(w, h); // RenderHelper.ChooseSwapExtent(w, h, swSupport.Capabilities);

        uint maxImageCount = 0;
        uint minImageCount = 1;

        maxImageCount = maxImageCount == 0 ? int.MaxValue : maxImageCount;
        imageCount = (int)Math.Clamp(trgImageCount, minImageCount, maxImageCount);

        images = RenderHelper.CreateImages(data.vk, data.deviceQ, imageCount, 1000, 1000, format, out imagesMemory);
        imageViews = RenderHelper.CreateImageViews(data.vk, data.deviceQ, [.. images], format);
        frameBuffers = RenderHelper.CreateFramebuffers(data.vk, data.deviceQ, [.. imageViews], data.renderPass, extent);
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
