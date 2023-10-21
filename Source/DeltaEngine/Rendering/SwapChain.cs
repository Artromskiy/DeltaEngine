﻿using Silk.NET.Vulkan;
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

    public uint imageCount;

    public readonly KhrSwapchain khrSw;
    public readonly SwapchainKHR swapChain;

    private readonly Renderer.RendererData data;

    public unsafe SwapChain(Api api, Renderer.RendererData data, RenderPass rp, int width, int height, uint trgImageCount)
    {
        this.data = data;
        var swSupport = data.swapChainSupport;
        var indiciesDetails = data.indiciesDetails;

        format = RenderHelper.ChooseSwapSurfaceFormat(swSupport.Formats);
        var presentMode = RenderHelper.ChoosePresentMode(swSupport.PresentModes);
        extent = RenderHelper.ChooseSwapExtent(width, height, swSupport.Capabilities);

        uint maxImageCount = swSupport.Capabilities.MaxImageCount;
        maxImageCount = maxImageCount == 0 ? int.MaxValue : maxImageCount;
        imageCount = Math.Clamp(trgImageCount, swSupport.Capabilities.MinImageCount, maxImageCount);

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

        _ = api.vk.TryGetDeviceExtension(data.instance, data.device, out khrSw);
        _ = khrSw.CreateSwapchain(data.device, creatInfo, null, out swapChain);
        uint imCount = imageCount;
        _ = khrSw.GetSwapchainImages(data.device, swapChain, &imCount, null);
        Span<Image> imageSpan = stackalloc Image[(int)imCount];
        _ = khrSw.GetSwapchainImages(data.device, swapChain, &imCount, imageSpan);
        images = ImmutableArray.Create(imageSpan);
        imageViews = RenderHelper.CreateImageViews(api, data.device, images.AsSpan(), format.Format);
        frameBuffers = RenderHelper.CreateFramebuffers(api, data.device, imageViews.AsSpan(), rp, extent);
    }

    public unsafe void Dispose()
    {
        data.vk.DeviceWaitIdle(data.device);

        foreach (var framebuffer in frameBuffers)
            data.vk.DestroyFramebuffer(data.device, framebuffer, null);
        //foreach (var image in images)
        //    data.vk.DestroyImage(data.device, image, null);
        foreach (var imageView in imageViews)
            data.vk.DestroyImageView(data.device, imageView, null);

        khrSw.DestroySwapchain(data.device, swapChain, null);
    }
}
