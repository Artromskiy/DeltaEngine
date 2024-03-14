using Delta.ECS;
using JobScheduler;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Delta.Rendering;

internal abstract unsafe partial class BaseRenderer : IDisposable, IJob
{
    private readonly Api _api;
    private readonly Window* _window;
    internal readonly RenderBase _rendererData;

    private readonly string _appName;

    private readonly Pipeline graphicsPipeline;
    private SwapChain swapChain;

    private readonly string[] deviceExtensions = [KhrSwapchain.ExtensionName];
    private readonly SurfaceFormatKHR targetFormat = new(Format.B8G8R8A8Srgb, ColorSpaceKHR.SpaceAdobergbLinearExt);

    private readonly RenderAssets renderAssets;

    private readonly List<(Render rend, uint count)> renderList = [];

    private readonly Queue<Frame> _frames = [];

    private const uint Buffering = 3;
    private const bool CanSkipRender = true;
    private const bool RenderLessMode = false;

    private bool _skippedFrame = true;

    public unsafe BaseRenderer(string appName)
    {
        _appName = appName;
        _api = new();
        _window = RenderHelper.CreateWindow(_api.sdl, _appName);
        _rendererData = new RenderBase(_api, _window, deviceExtensions, _appName, targetFormat);
        swapChain = new SwapChain(_api, _rendererData, GetSdlWindowSize(), Buffering, _rendererData.format);
        graphicsPipeline = RenderHelper.CreateGraphicsPipeline(_rendererData);
        renderAssets = new RenderAssets(_rendererData);

        for (int i = 0; i < swapChain.imageCount; i++)
            _frames.Enqueue(new Frame(_rendererData, renderAssets, swapChain));
    }

    private Frame CurrentFrame => _frames.Peek();

    private void Sync()
    {
        _api.sdl.PumpEvents();
        if (!_skippedFrame)
            _frames.Enqueue(_frames.Dequeue());

        PreSync();

        if (_skippedFrame = RenderLessMode || (CanSkipRender && !CurrentFrame.Synced()))
            return;

        CurrentFrame.Sync();

        PostSync();
    }

    /// <summary>
    /// Use for updating data from global graphics buffers to appropriate frame buffer asyncronously
    /// </summary>
    public abstract void PostSync();

    /// <summary>
    /// Use for updating data from game context to global graphics buffers
    /// </summary>
    public abstract void PreSync();

    public void Execute()
    {
        Sync();
        Draw();
    }

    private void Draw()
    {
        if (_skippedFrame)
        {
            return;
        }

        CurrentFrame.Draw(renderList, out var resize);
        if (resize)
            OnResize();
    }

    internal DynamicBuffer GetTRSBuffer() => CurrentFrame.GetTRSBuffer();
    internal DynamicBuffer GetIdsBuffer() => CurrentFrame.GetIdsBuffer();
    protected void AddSemaphore(Semaphore semaphore) => CurrentFrame.AddSemaphore(semaphore);
    protected void SetRenders(ReadOnlySpan<(Render rend, uint count)> renders)
    {
        renderList.Clear();
        renderList.AddRange(renders);
    }

    public unsafe void Dispose()
    {
        _frames.Dispose();
        _rendererData.vk.DestroyPipeline(_rendererData.deviceQ.device, graphicsPipeline, null);

        swapChain.Dispose();
        _rendererData.Dispose();

        _api.sdl.DestroyWindow(_window);
        _api.sdl.Dispose();
    }


    private unsafe (int w, int h) GetSdlWindowSize()
    {
        int w, h;
        w = h = 0;
        _api.sdl.VulkanGetDrawableSize(_window, ref w, ref h);
        return (w, h);
    }

    private void OnResize()
    {
        swapChain.Dispose();
        _rendererData.UpdateSupportDetails();
        swapChain = new SwapChain(_api, _rendererData, GetSdlWindowSize(), 3, _rendererData.format);

        if (swapChain.imageCount == _frames.Count)
        {
            foreach (var frame in _frames)
                frame.UpdateSwapChain(swapChain);
            return;
        }

        _frames.Dispose();
        _frames.Clear();
        for (int i = 0; i < swapChain.imageCount; i++)
            _frames.Enqueue(new Frame(_rendererData, renderAssets, swapChain));
    }
}