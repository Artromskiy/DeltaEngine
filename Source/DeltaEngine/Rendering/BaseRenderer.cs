using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Diagnostics;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace DeltaEngine.Rendering;

public abstract unsafe class BaseRenderer : IDisposable
{
    private readonly Api _api;

    private readonly Window* _window;

    internal readonly RenderBase _rendererData;

    private const string RendererName = "Delta Renderer";
    private readonly string _appName;

    private Pipeline graphicsPipeline;

    private SwapChain swapChain;
    private DescriptorSetLayout descriptorSetLayout;

    private RenderPass renderPass;
    private PipelineLayout pipelineLayout;

    private int _currentFrame = 0;

    private Frame[] _frames;

    private readonly string[] deviceExtensions = [KhrSwapchain.ExtensionName];

    private readonly SurfaceFormatKHR targetFormat = new(Format.B8G8R8A8Srgb, ColorSpaceKHR.SpaceAdobergbLinearExt);

    private Silk.NET.SDL.Event emptySdlEvent = new();

    private const uint Buffering = 3;

    private const bool CanSkipRender = false;
    private const bool RenderLessMode = true;

    private bool _skippedFrame = true;

    public unsafe BaseRenderer(string appName)
    {
        _appName = appName;
        _api = new();
        _window = RenderHelper.CreateWindow(_api.sdl, _appName);
        _rendererData = new RenderBase(_api, _window, deviceExtensions, _appName, RendererName, targetFormat);
        var count = _rendererData.vk.GetPhysicalDeviceProperties(_rendererData.gpu).Limits.MaxVertexInputAttributes;
        renderPass = RenderHelper.CreateRenderPass(_api, _rendererData.deviceQueues.device, _rendererData.format.Format);
        swapChain = new SwapChain(_api, _rendererData, renderPass, GetSdlWindowSize(), Buffering, _rendererData.format);
        descriptorSetLayout = RenderHelper.CreateDescriptorSetLayout(_rendererData);
        (graphicsPipeline, pipelineLayout) = RenderHelper.CreateGraphicsPipeline(_rendererData, renderPass, [descriptorSetLayout]);

        _frames = new Frame[swapChain.imageCount];
        for (int i = 0; i < swapChain.imageCount; i++)
            _frames[i] = new Frame(_rendererData, swapChain, renderPass, descriptorSetLayout);
    }

    private readonly Stopwatch _waitSync = new();

    public virtual void ClearCounters()
    {
        _waitSync.Reset();
        for (int i = 0; i < _frames.Length; i++)
            _frames[i].ClearMetrics();
    }
    public TimeSpan GetSyncMetric => _waitSync.Elapsed;
    public TimeSpan GetAcquireMetric()
    {
        TimeSpan res = TimeSpan.Zero;
        for (int i = 0; i < _frames.Length; i++)
            res += _frames[i].AcquireMetric;
        return res;
    }

    public void Sync()
    {
        _api.sdl.PumpEvents();
        if (!_skippedFrame)
            _currentFrame = (_currentFrame + 1) % _frames.Length;

        PreSync();

        _skippedFrame = RenderLessMode || (CanSkipRender && !_frames[_currentFrame].Synced());

        if (_skippedFrame)
            return;

        _waitSync.Start();
        _frames[_currentFrame].Sync();
        _waitSync.Stop();

        PostSync();
    }

    /// <summary>
    /// Use for updating data from global graphics buffers to appropriate buffer asyncronously
    /// </summary>
    public abstract void PostSync();

    /// <summary>
    /// Use for updating data from game context to global graphics buffers
    /// </summary>
    public abstract void PreSync();

    public void Run()
    {
        if (_skippedFrame)
            return;

        _frames[_currentFrame].Draw(graphicsPipeline, pipelineLayout, out var resize);
        if (resize)
            OnResize();
    }

    internal DynamicBuffer GetTRSBuffer()
    {
        return _frames[_currentFrame].GetTRSBuffer();
    }

    protected void SetBuffers(Buffer vbo, Buffer ibo, uint indicesCount, uint verticesCount)
    {
        _frames[_currentFrame].SetBuffers(vbo, ibo, indicesCount, verticesCount);
    }

    protected void SetInstanceCount(uint instances)
    {
        _frames[_currentFrame].SetInstanceCount(instances);
    }

    protected void AddSemaphore(Semaphore semaphore)
    {
        _frames[_currentFrame].AddSemaphore(semaphore);
    }

    public unsafe void Dispose()
    {
        foreach (var frame in _frames)
            frame.Dispose();
        _rendererData.vk.DestroyPipeline(_rendererData.deviceQueues.device, graphicsPipeline, null);
        _rendererData.vk.DestroyPipelineLayout(_rendererData.deviceQueues.device, pipelineLayout, null);
        _rendererData.vk.DestroyRenderPass(_rendererData.deviceQueues.device, renderPass, null);

        swapChain.Dispose();
        _rendererData.Dispose();
        _rendererData.vk.Dispose();

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
        swapChain = new SwapChain(_api, _rendererData, renderPass, GetSdlWindowSize(), 3, _rendererData.format);

        if (swapChain.imageCount != _frames.Length)
        {
            foreach (var frame in _frames)
                frame.Dispose();
            _frames = new Frame[swapChain.imageCount];
            for (int i = 0; i < swapChain.imageCount; i++)
                _frames[i] = new Frame(_rendererData, swapChain, renderPass, descriptorSetLayout);
            return;
        }
        foreach (var frame in _frames)
            frame.UpdateSwapChain(swapChain);
    }
}