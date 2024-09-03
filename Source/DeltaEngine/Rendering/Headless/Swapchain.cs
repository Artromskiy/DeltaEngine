using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using System;
using System.Collections.Immutable;
using System.IO;

namespace Delta.Rendering.Headless;
internal class SwapChain : IDisposable
{
    public readonly ImmutableArray<DeviceMemory> imagesMemory;
    public readonly ImmutableArray<Image> images;
    public readonly ImmutableArray<ImageView> imageViews;
    public readonly ImmutableArray<Framebuffer> frameBuffers;
    public readonly Stream RenderStream;

    private readonly Image _hostImage;
    private readonly DeviceMemory _hostMemory;
    private readonly unsafe nint _imageData;

    private Fence _copyFence;
    private SubresourceLayout _subResourceLayout;
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
        RenderStream = new MemoryStream(width * height * 4);

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

        (_hostImage, _hostMemory) = RenderHelper.CreateImage(
            data.vk, data.deviceQ, width, height, format,
            ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferDstBit,
            MemoryPropertyFlags.HostVisibleBit,
            ImageTiling.Linear);

        ImageSubresource subResource = new(ImageAspectFlags.ColorBit);
        data.vk.GetImageSubresourceLayout(data.deviceQ, _hostImage, subResource, out _subResourceLayout);

        void* pdata = default;
        _ = data.vk.MapMemory(data.deviceQ, _hostMemory, 0, Vk.WholeSize, 0, &pdata);
        _imageData = new(pdata);
        _imageData += (nint)_subResourceLayout.Offset;
    }

    public void Present(Semaphore waitSemaphore)
    {
        RenderHelper.CopyImage(data.vk, _cmdBuffer, data.deviceQ,
            images[_currentFrameIndex], _hostImage, width, height,
            waitSemaphore, _copyFence);
        data.vk.WaitForFences(data.deviceQ, 1, _copyFence, true, ulong.MaxValue);
        CopyData(RenderStream);
        data.vk.ResetFences(data.deviceQ, 1, _copyFence);
    }

    private unsafe void CopyData(Stream RenderStream)
    {
        var rowPtr = _imageData;
        int rowPitch = (int)_subResourceLayout.RowPitch;
        int size = (int)_subResourceLayout.ArrayPitch;
        Span<byte> colors = new(rowPtr.ToPointer(), size);
        RenderStream.Position = 0;
        for (int i = 0; i < height; i++)
            RenderStream.Write(colors.Slice(i * rowPitch, width * 4));
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

        RenderStream.Dispose();
    }
}
