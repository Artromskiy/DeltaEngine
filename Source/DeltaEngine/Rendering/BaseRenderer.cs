using JobScheduler;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Delta.Rendering;

public abstract unsafe partial class BaseRenderer : IDisposable, IJob
{
    private readonly Api _api;
    private readonly Window* _window;
    internal readonly RenderBase _rendererData;

    private readonly string _appName;

    private readonly Pipeline graphicsPipeline;
    private readonly DescriptorSetLayout descriptorSetLayout;
    private SwapChain swapChain;

    private readonly RenderPass renderPass;
    private readonly PipelineLayout pipelineLayout;

    private readonly string[] deviceExtensions = [KhrSwapchain.ExtensionName];
    private readonly SurfaceFormatKHR targetFormat = new(Format.B8G8R8A8Srgb, ColorSpaceKHR.SpaceAdobergbLinearExt);

    private readonly Queue<Frame> _frames = [];

    private const uint Buffering = 1;
    private const bool CanSkipRender = true;
    private const bool RenderLessMode = false;

    private bool _skippedFrame = true;

    public unsafe BaseRenderer(string appName)
    {
        _appName = appName;
        _api = new();
        _window = RenderHelper.CreateWindow(_api.sdl, _appName);
        _rendererData = new RenderBase(_api, _window, deviceExtensions, _appName, targetFormat);
        renderPass = RenderHelper.CreateRenderPass(_api, _rendererData.deviceQ.device, _rendererData.format.Format);
        swapChain = new SwapChain(_api, _rendererData, renderPass, GetSdlWindowSize(), Buffering, _rendererData.format);
        descriptorSetLayout = RenderHelper.CreateDescriptorSetLayout(_rendererData);
        (graphicsPipeline, pipelineLayout) = RenderHelper.CreateGraphicsPipeline(_rendererData, renderPass, [descriptorSetLayout]);

        for (int i = 0; i < swapChain.imageCount; i++)
            _frames.Enqueue(new Frame(_rendererData, swapChain, renderPass, descriptorSetLayout));
    }

    private Frame CurrentFrame => _frames.Peek();

    public void Sync()
    {
        _api.sdl.PumpEvents();
        if (!_skippedFrame)
            _frames.Enqueue(_frames.Dequeue());

        PreSync();

        if (_skippedFrame = RenderLessMode || (CanSkipRender && !CurrentFrame.Synced()))
            return;

        _waitSync.Start();
        CurrentFrame.Sync();
        _waitSync.Stop();

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
        _framesCount++;

        if (_skippedFrame)
        {
            _framesSkip++;
            return;
        }

        CurrentFrame.Draw(graphicsPipeline, pipelineLayout, out var resize);
        if (resize)
            OnResize();
    }

    internal DynamicBuffer GetTRSBuffer() => CurrentFrame.GetTRSBuffer();
    internal DynamicBuffer GetParentsBuffer() => CurrentFrame.GEtParentsBuffer();
    protected void SetBuffers(Buffer vbo, Buffer ibo, uint indicesCount, uint verticesCount) => CurrentFrame.SetBuffers(vbo, ibo, indicesCount, verticesCount);
    protected void SetInstanceCount(uint instances) => CurrentFrame.SetInstanceCount(instances);
    protected void AddSemaphore(Semaphore semaphore) => CurrentFrame.AddSemaphore(semaphore);

    public unsafe void Dispose()
    {
        _frames.Dispose();
        _rendererData.vk.DestroyPipeline(_rendererData.deviceQ.device, graphicsPipeline, null);
        _rendererData.vk.DestroyPipelineLayout(_rendererData.deviceQ.device, pipelineLayout, null);
        _rendererData.vk.DestroyRenderPass(_rendererData.deviceQ.device, renderPass, null);

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
        swapChain = new SwapChain(_api, _rendererData, renderPass, GetSdlWindowSize(), 3, _rendererData.format);

        if (swapChain.imageCount == _frames.Count)
        {
            foreach (var frame in _frames)
                frame.UpdateSwapChain(swapChain);
            return;
        }

        _frames.Dispose();
        _frames.Clear();
        for (int i = 0; i < swapChain.imageCount; i++)
            _frames.Enqueue(new Frame(_rendererData, swapChain, renderPass, descriptorSetLayout));
    }
}