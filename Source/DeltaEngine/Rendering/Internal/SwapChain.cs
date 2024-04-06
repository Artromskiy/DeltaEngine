using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Immutable;

namespace Delta.Rendering.Internal;
internal class SwapChain : IDisposable
{
    public readonly ImmutableArray<Image> images;
    public readonly ImmutableArray<ImageView> imageViews;
    public readonly ImmutableArray<Framebuffer> frameBuffers;
    public readonly SurfaceFormatKHR format;
    public Extent2D extent;

    public int imageCount;

    public readonly KhrSwapchain khrSw;
    public readonly SwapchainKHR swapChain;

    private readonly RenderBase data;

    public unsafe SwapChain(Api api, RenderBase data, (int w, int h) size, uint trgImageCount, SurfaceFormatKHR targetFormat)
    {
        this.data = data;
        var swSupport = data.swapChainSupport;
        var indiciesDetails = data.deviceQ.queueIndicesDetails;

        format = RenderHelper.ChooseSwapSurfaceFormat(swSupport.Formats, targetFormat);
        var presentMode = PresentModeKHR.MailboxKhr; // swSupport.PresentModes.Contains(PresentModeKHR.ImmediateKhr) ? PresentModeKHR.ImmediateKhr : PresentModeKHR.FifoKhr;
        extent = RenderHelper.ChooseSwapExtent(size.w, size.h, swSupport.Capabilities);

        uint maxImageCount = swSupport.Capabilities.MaxImageCount;
        maxImageCount = maxImageCount == 0 ? int.MaxValue : maxImageCount;
        imageCount = (int)Math.Clamp(trgImageCount, swSupport.Capabilities.MinImageCount, maxImageCount);

        bool sameFamily = indiciesDetails.graphicsFamily == indiciesDetails.presentFamily;

        var queueFamilyIndices = stackalloc[] { indiciesDetails.graphicsFamily, indiciesDetails.presentFamily };

        SwapchainCreateInfoKHR creatInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = data.surface,
            MinImageCount = (uint)imageCount,
            ImageFormat = format.Format,
            ImageColorSpace = format.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            ImageSharingMode = sameFamily ? SharingMode.Exclusive : SharingMode.Concurrent,
            QueueFamilyIndexCount = sameFamily ? 0u : 2u,
            PQueueFamilyIndices = sameFamily ? null : queueFamilyIndices,
            PreTransform = swSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = default,
            Flags = SwapchainCreateFlagsKHR.None
        };

        _ = api.vk.TryGetDeviceExtension(data.instance, data.deviceQ, out khrSw);
        _ = khrSw.CreateSwapchain(data.deviceQ, creatInfo, null, out swapChain);
        uint imCount = (uint)imageCount;
        _ = khrSw.GetSwapchainImages(data.deviceQ, swapChain, &imCount, null);
        Span<Image> imageSpan = stackalloc Image[(int)imCount];
        _ = khrSw.GetSwapchainImages(data.deviceQ, swapChain, &imCount, imageSpan);
        images = ImmutableArray.Create(imageSpan);
        imageViews = RenderHelper.CreateImageViews(api, data.deviceQ, [.. images], format.Format);
        frameBuffers = RenderHelper.CreateFramebuffers(api, data.deviceQ, [.. imageViews], data.renderPass, extent);
    }

    public unsafe void Dispose()
    {
        data.vk.DeviceWaitIdle(data.deviceQ);

        foreach (var framebuffer in frameBuffers)
            data.vk.DestroyFramebuffer(data.deviceQ, framebuffer, null);
        foreach (var imageView in imageViews)
            data.vk.DestroyImageView(data.deviceQ, imageView, null);

        khrSw.DestroySwapchain(data.deviceQ, swapChain, null);
    }
}
