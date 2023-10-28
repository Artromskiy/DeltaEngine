using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Threading;
using static DeltaEngine.ThrowHelper;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;
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

    private Pipeline? graphicsPipeline;

    private SwapChain swapChain;

    private RenderPass renderPass;
    private PipelineLayout pipelineLayout;
    private readonly CommandBuffer[] commandBuffers;

    private readonly Semaphore[] imageAvailableSemaphores;
    private readonly Semaphore[] renderFinishedSemaphores;
    private readonly Fence[] inFlightFences;
    private int currentFrame = 0;


    private readonly string[] deviceExtensions = new[]
    {
        KhrSwapchain.ExtensionName,
    };

    private readonly Buffer _vertexBuffer;
    private readonly DeviceMemory _vertexBufferMemory;


    private readonly Buffer _indexBuffer;
    private readonly DeviceMemory _indexBufferMemory;


    private readonly Vertex[] triangleVertices = 
    {
        new (new(0, -0.5f), new(1.0f, 1.0f, 1.0f)),
        new (new(0.5f, 0), new(0.0f, 0.0f, 1.0f)),
        new (new(0, 0.5f), new(0.0f, 1.0f, 0.0f)),
        new (new(-0.5f, 0f), new(1.0f, 0.0f, 0.0f)),
    };
    private readonly uint[] indices = 
    {
        0, 1, 2, 2, 3, 0
    };

    public unsafe Renderer(string appName)
    {
        _appName = appName;
        _api = new();
        _window = RenderHelper.CreateWindow(_api.sdl, _appName);
        _rendererData = new RenderBase(_api, _window, deviceExtensions, _appName, RendererName);
        var count = _rendererData.vk.GetPhysicalDeviceProperties(_rendererData.gpu).Limits.MaxVertexInputAttributes;
        renderPass = RenderHelper.CreateRenderPass(_api, _rendererData.device, _rendererData.format.Format);
        swapChain = new SwapChain(_api, _rendererData, renderPass, GetSdlWindowSize(), 3);
        (graphicsPipeline, pipelineLayout) = RenderHelper.CreateGraphicsPipeline(_rendererData, swapChain.extent, renderPass);
        (_vertexBuffer, _vertexBufferMemory) = RenderHelper.CreateVertexBuffer(_rendererData, triangleVertices);
        (_indexBuffer, _indexBufferMemory) = RenderHelper.CreateIndexBuffer(_rendererData, indices);
        commandBuffers = RenderHelper.CreateCommandBuffers(_rendererData, swapChain.imageCount);
        (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences) = RenderHelper.CreateSyncObjects(_api, _rendererData.device, swapChain.imageCount);
        (_drawThread = new Thread(new ThreadStart(DrawLoop))).Start();
        _drawThread.Name = RendererName;
    }

    private void DrawLoop()
    {
        while (_drawEvent.WaitOne())
            Draw();
    }

    public void SendDrawEvent()
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
        foreach (var semaphore in renderFinishedSemaphores)
            _api.vk.DestroySemaphore(_rendererData.device, semaphore, null);
        foreach (var semaphore in imageAvailableSemaphores)
            _api.vk.DestroySemaphore(_rendererData.device, semaphore, null);
        foreach (var fence in inFlightFences)
            _api.vk.DestroyFence(_rendererData.device, fence, null);
        _api.vk.DestroyCommandPool(_rendererData.device, _rendererData.commandPool, null);

        if (graphicsPipeline.HasValue)
            _api.vk.DestroyPipeline(_rendererData.device, graphicsPipeline.Value, null);

        _api.vk.DestroyPipelineLayout(_rendererData.device, pipelineLayout, null);
        _api.vk.DestroyRenderPass(_rendererData.device, renderPass, null);

        swapChain.Dispose();

        _api.vk.DestroyDevice(_rendererData.device, null);

        _api.vk.TryGetInstanceExtension(_rendererData.instance, out KhrSurface khrsf);
        khrsf.DestroySurface(_rendererData.instance, _rendererData.surface, null);
        _api.vk.DestroyInstance(_rendererData.instance, null);
        _api.vk.Dispose();

        _api.sdl.DestroyWindow(_window);
        _api.sdl.Dispose();
    }

    public unsafe void Run()
    {
        _api.sdl.PollEvent((Silk.NET.SDL.Event*)null);
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
        _rendererData.vk.WaitForFences(_rendererData.device, 1, inFlightFences[currentFrame], true, ulong.MaxValue);

        var waitSemaphores = stackalloc[] { imageAvailableSemaphores[currentFrame] };

        uint imageIndex = 0;
        var res = swapChain.khrSw.AcquireNextImage(_rendererData.device, swapChain.swapChain, ulong.MaxValue, *waitSemaphores, default, ref imageIndex);

        if (res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr)
        {
            OnResize();
            return;
        }

        _rendererData.vk.ResetFences(_rendererData.device, 1, inFlightFences[currentFrame]);

        _rendererData.vk.ResetCommandBuffer(commandBuffers[currentFrame], 0);
        RecordCommandBuffer(commandBuffers[currentFrame], imageIndex);

        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };
        var signalSemaphores = stackalloc[] { renderFinishedSemaphores[currentFrame] };

        var buffer = commandBuffers[imageIndex];
        SubmitInfo submitInfo = new()
        {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };
        _ = _rendererData.vk.QueueSubmit(_rendererData.graphicsQueue, 1, submitInfo, inFlightFences[currentFrame]);

        var swapChains = stackalloc[] { swapChain.swapChain };
        PresentInfoKHR presentInfo = new()
        {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,
            SwapchainCount = 1,
            PSwapchains = swapChains,
            PImageIndices = &imageIndex
        };
        res = swapChain.khrSw.QueuePresent(_rendererData.presentQueue, presentInfo);

        if (res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr)
            OnResize();

        currentFrame = (currentFrame + 1) % swapChain.imageCount;
    }

    private void OnResize()
    {
        swapChain.Dispose();
        _rendererData.UpdateSupportDetails();
        swapChain = new SwapChain(_api, _rendererData, renderPass, GetSdlWindowSize(), 3);
    }

    private unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex)
    {
        CommandBufferBeginInfo beginInfo = new(StructureType.CommandBufferBeginInfo);
        ClearValue clearColor = new(new ClearColorValue(0.05f, 0.05f, 0.05f, 1));

        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderPass,
            Framebuffer = swapChain.frameBuffers[(int)imageIndex],
            ClearValueCount = 1,
            PClearValues = &clearColor,
            RenderArea = new Rect2D(extent: swapChain.extent)
        };

        Viewport viewport = new()
        {
            Width = swapChain.extent.Width,
            Height = swapChain.extent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };

        Rect2D scissor = new(extent: swapChain.extent);

        _ = _rendererData.vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        _rendererData.vk.CmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);
        if (graphicsPipeline.HasValue)
            _rendererData.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, graphicsPipeline.Value);
        _rendererData.vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        _rendererData.vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);

        var buffer = stackalloc Buffer[] { _vertexBuffer };
        ulong offsets = 0;

        _rendererData.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, buffer, &offsets);
        _rendererData.vk.CmdBindIndexBuffer(commandBuffer, _indexBuffer, 0, IndexType.Uint32);
        _rendererData.vk.CmdDrawIndexed(commandBuffer, (uint)indices.Length, 1, 0, 0, 0);
        _rendererData.vk.CmdEndRenderPass(commandBuffer);

        _ = _rendererData.vk.EndCommandBuffer(commandBuffer);
    }


}
