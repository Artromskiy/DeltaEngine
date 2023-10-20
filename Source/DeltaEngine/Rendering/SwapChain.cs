using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Immutable;

namespace DeltaEngine.Rendering;
internal class SwapChain : IDisposable
{
    public readonly ImmutableArray<Image> images;
    public readonly ImmutableArray<ImageView> imageViews;
    public readonly ImmutableArray<Framebuffer> frameBuffers;
    public readonly SurfaceFormatKHR format;
    public Extent2D extent;

    public readonly SwapchainKHR swapChain;

    private readonly Renderer.RendererData data;

    public unsafe SwapChain(Api api, Renderer.RendererData data, RenderPass rp, int width, int height)
    {
        this.data = data;
        var swSupport = data.swapChainSupport;
        var indiciesDetails = data.indiciesDetails;

        format = RenderHelper.ChooseSwapSurfaceFormat(swSupport.Formats);
        var presentMode = RenderHelper.ChoosePresentMode(swSupport.PresentModes);
        extent = RenderHelper.ChooseSwapExtent(width, height, swSupport.Capabilities);

        var imageCount = swSupport.Capabilities.MinImageCount + 1;
        if (swSupport.Capabilities.MaxImageCount > 0 && imageCount > swSupport.Capabilities.MaxImageCount)
            imageCount = swSupport.Capabilities.MaxImageCount;

        bool sameFamily = indiciesDetails.graphicsFamily == indiciesDetails.presentFamily;

        var queueFamilyIndices = stackalloc[] { indiciesDetails.graphicsFamily, indiciesDetails.presentFamily };

        SwapchainCreateInfoKHR creatInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = data.surface,
            MinImageCount = imageCount,
            ImageFormat = format.Format,
            ImageColorSpace = format.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            ImageSharingMode = sameFamily ? SharingMode.Exclusive : SharingMode.Concurrent,
            QueueFamilyIndexCount = sameFamily ? 0u : 2u,
            PQueueFamilyIndices = sameFamily ? null : queueFamilyIndices,
            PreTransform =  swSupport.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = presentMode,
            Clipped = true,
            OldSwapchain = default
        };

        _ = api.vk.TryGetDeviceExtension<KhrSwapchain>(data.instance, data.device, out var khrsw);
        _ = khrsw.CreateSwapchain(data.device, creatInfo, null, out swapChain);

        _ = khrsw.GetSwapchainImages(data.device, swapChain, &imageCount, null);
        Span<Image> imageSpan = stackalloc Image[(int)imageCount];
        _ = khrsw.GetSwapchainImages(data.device, swapChain, &imageCount, imageSpan);
        images = ImmutableArray.Create(imageSpan);
        imageViews = RenderHelper.CreateImageViews(api, data.device, images.AsSpan(), format.Format);
        frameBuffers = RenderHelper.CreateFramebuffers(api, data.device, imageViews.AsSpan(), rp, extent);
    }

    public unsafe void Dispose()
    {
        foreach (var framebuffer in frameBuffers)
            data.vk.DestroyFramebuffer(data.device, framebuffer, null);
        foreach (var image in images)
            data.vk.DestroyImage(data.device, image, null);
        foreach (var imageView in imageViews)
            data.vk.DestroyImageView(data.device, imageView, null);

        _ = data.vk.TryGetDeviceExtension(data.instance, data.device, out KhrSwapchain khrsw);
        khrsw.DestroySwapchain(data.device, swapChain, null);
    }
}
