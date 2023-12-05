using DeltaEngine.ECS;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Numerics;
using System.Threading;

using Buffer = Silk.NET.Vulkan.Buffer;
using Thread = System.Threading.Thread;

namespace DeltaEngine.Rendering;

public sealed unsafe class Renderer : IDisposable
{
    private readonly Api _api;

    public readonly Window* _window;

    private readonly RenderBase _rendererData;

    private readonly AutoResetEvent _drawEvent = new(false);
    private readonly Thread _drawThread;

    private const string RendererName = "Delta Renderer";
    private readonly string _appName;

    private Pipeline graphicsPipeline;

    private SwapChain swapChain;

    private RenderPass renderPass;
    private PipelineLayout pipelineLayout;

    private int currentFrame = 0;

    private Frame[] _frames;

    private readonly string[] deviceExtensions = new[]
    {
        KhrSwapchain.ExtensionName,
    };

    private readonly Buffer _vertexBuffer;
    private readonly DeviceMemory _vertexBufferMemory;


    private readonly Buffer _indexBuffer;
    private readonly DeviceMemory _indexBufferMemory;

    private readonly SurfaceFormatKHR targetFormat = new(Format.B8G8R8A8Srgb, ColorSpaceKHR.SpaceAdobergbLinearExt);

    private static readonly Vector3 r = new(1.0f, 0.0f, 0.0f);
    private static readonly Vector3 g = new(0.0f, 1.0f, 0.0f);
    private static readonly Vector3 b = new(0.0f, 0.0f, 1.0f);

    private readonly Vertex[] deltaLetterVertices =
    {
        new (new(0.0f, -0.5f),   b),
        new (new(0.6f, 0.5f),    g),
        new (new(-0.6f, 0.5f),   r),
        new (new(0.0f, -0.25f),   r),
        new (new(0.35f, 0.35f),    b),
        new (new(-0.35f, 0.35f),   g),
    };
    private readonly uint[] deltaLetterIndices =
    {
        0,1,3,
        1,2,4,
        2,0,5,
        3,1,4,
        4,2,5,
        5,0,3
    };

    public unsafe Renderer(string appName)
    {
        _appName = appName;
        _api = new();
        _window = RenderHelper.CreateWindow(_api.sdl, _appName);
        _rendererData = new RenderBase(_api, _window, deviceExtensions, _appName, RendererName, targetFormat);
        var count = _rendererData.vk.GetPhysicalDeviceProperties(_rendererData.gpu).Limits.MaxVertexInputAttributes;
        renderPass = RenderHelper.CreateRenderPass(_api, _rendererData.device, _rendererData.format.Format);
        swapChain = new SwapChain(_api, _rendererData, renderPass, GetSdlWindowSize(), 3, _rendererData.format);
        //var p = RenderHelper.CreateDescriptorSet(_rendererData);
        (graphicsPipeline, pipelineLayout) = RenderHelper.CreateGraphicsPipeline(_rendererData, renderPass, Array.Empty<DescriptorSetLayout>());
        (_vertexBuffer, _vertexBufferMemory) = RenderHelper.CreateVertexBuffer(_rendererData, deltaLetterVertices);
        (_indexBuffer, _indexBufferMemory) = RenderHelper.CreateIndexBuffer(_rendererData, deltaLetterIndices);

        _frames = new Frame[swapChain.imageCount];
        for (int i = 0; i < swapChain.imageCount; i++)
            _frames[i] = new Frame(_rendererData, swapChain);

        (_drawThread = new Thread(new ThreadStart(DrawLoop))).Start();
        _drawThread.Name = RendererName;
    }

    private void DrawLoop()
    {
        while (_drawEvent.WaitOne())
            Draw();
    }

    public void SubmitDraw()
    {
        _drawEvent.Set();
    }

    internal void SubmitDraw(RenderData[] toRender)
    {
        _drawEvent.Set();
    }

    public unsafe void SetWindowPositionAndSize((int x, int y, int w, int h) rect)
    {
        _api.sdl.SetWindowPosition(_window, rect.x, rect.y);
        _api.sdl.SetWindowSize(_window, rect.w, rect.h);
    }

    public unsafe void Dispose()
    {
        foreach (var frame in _frames)
            frame.Dispose();
        _rendererData.vk.DestroyCommandPool(_rendererData.device, _rendererData.commandPool, null);
        _rendererData.vk.DestroyPipeline(_rendererData.device, graphicsPipeline, null);
        _rendererData.vk.DestroyPipelineLayout(_rendererData.device, pipelineLayout, null);
        _rendererData.vk.DestroyRenderPass(_rendererData.device, renderPass, null);

        swapChain.Dispose();
        _rendererData.Dispose();
        _rendererData.vk.Dispose();

        _api.sdl.DestroyWindow(_window);
        _api.sdl.Dispose();
    }

    Silk.NET.SDL.Event emptySdlEvent = new();
    public unsafe void Run()
    {
        //_api.sdl.PollEvent(ref emptySdlEvent);
        _api.sdl.PumpEvents();
    }

    private unsafe (int w, int h) GetSdlWindowSize()
    {
        int w, h;
        w = h = 0;
        _api.sdl.VulkanGetDrawableSize(_window, ref w, ref h);
        return (w, h);
    }

    private unsafe void Draw()
    {
        _frames[currentFrame].Draw(renderPass, graphicsPipeline, _vertexBuffer, _indexBuffer, (uint)deltaLetterIndices.Length, out var resize);
        if (resize)
        {
            OnResize();
            return;
        }
        currentFrame = (currentFrame + 1) % _frames.Length;
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
                _frames[i] = new Frame(_rendererData, swapChain);
            return;
        }
        foreach (var frame in _frames)
            frame.UpdateSwapChain(swapChain);
    }
}
