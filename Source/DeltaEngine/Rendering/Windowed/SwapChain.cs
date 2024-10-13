using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Immutable;

namespace Delta.Rendering.Windowed;
internal class SwapChain : IDisposable
{
    public readonly ImmutableArray<Image> images;
    public readonly ImmutableArray<ImageView> imageViews;
    public readonly ImmutableArray<Framebuffer> frameBuffers;
    public readonly SurfaceFormatKHR format;
    public Extent2D Extent { get; private set; }

    public int imageCount;

    public readonly KhrSwapchain khrSw;
    public readonly SwapchainKHR swapChain;

    private readonly RenderBase data;

    public unsafe SwapChain(RenderBase data, uint trgImageCount, SurfaceFormatKHR targetFormat, int width, int height)
    {
        this.data = data;
        var swSupport = data.SwapChainSupport;
        var queueFamilies = data.deviceQ.familyQueues;

        format = RenderHelper.ChooseSwapSurfaceFormat(swSupport.Formats, targetFormat);
        var presentMode = PresentModeKHR.MailboxKhr; // swSupport.PresentModes.Contains(PresentModeKHR.ImmediateKhr) ? PresentModeKHR.ImmediateKhr : PresentModeKHR.FifoKhr;

        Extent = RenderHelper.ChooseSwapExtent(width, height, swSupport.Capabilities);

        uint maxImageCount = swSupport.Capabilities.MaxImageCount;
        maxImageCount = maxImageCount == 0 ? int.MaxValue : maxImageCount;
        imageCount = (int)Math.Clamp(trgImageCount, swSupport.Capabilities.MinImageCount, maxImageCount);

        bool sameFamily = queueFamilies[QueueType.Graphics].family == queueFamilies[QueueType.Present].family;

        var queueFamilyIndices = stackalloc[] { queueFamilies[QueueType.Graphics].family, queueFamilies[QueueType.Present].family };
        _ = data.vk.TryGetDeviceExtension(data.instance, data.deviceQ, out khrSw);

        SwapchainCreateInfoKHR creatInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = data.Surface,
            MinImageCount = (uint)imageCount,
            ImageFormat = format.Format,
            ImageColorSpace = format.ColorSpace,
            ImageExtent = Extent,
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
        _ = khrSw.CreateSwapchain(data.deviceQ, creatInfo, null, out swapChain);
        uint imCount = (uint)imageCount;
        _ = khrSw.GetSwapchainImages(data.deviceQ, swapChain, &imCount, null);
        Span<Image> imageSpan = stackalloc Image[(int)imCount];
        _ = khrSw.GetSwapchainImages(data.deviceQ, swapChain, &imCount, imageSpan);
        images = ImmutableArray.Create(imageSpan);
        imageViews = RenderHelper.CreateImageViews(data.vk, data.deviceQ, [.. images], format.Format);
        frameBuffers = RenderHelper.CreateFramebuffers(data.vk, data.deviceQ, [.. imageViews], data.renderPass, (int)Extent.Width, (int)Extent.Height);
    }

    public unsafe uint GetImageIndex(Semaphore semaphore, out bool resize)
    {
        uint index = 0;
        var res = khrSw.AcquireNextImage(data.deviceQ, swapChain, ulong.MaxValue, semaphore, default, &index);
        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
        return index;
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
