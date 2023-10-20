using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using static DeltaEngine.ThrowHelper;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace DeltaEngine.Rendering;

internal sealed unsafe class Renderer : IDisposable
{
    private readonly Data _data;
    private readonly Api _api;

    private struct Data
    {
        public Window* _window;
        public Queue _queue;
    }

    private readonly RendererData _rendererData;

    private const string RendererName = "Delta Renderer";
    private readonly string _appName;

    private Pipeline? graphicsPipeline;

    private readonly SwapChain swapChain;

    private RenderPass renderPass;
    private PipelineLayout pipelineLayout;
    private CommandPool commandPool;
    private readonly CommandBuffer[] commandBuffers;

    private readonly Semaphore[] imageAvailableSemaphores;
    private readonly Semaphore[] renderFinishedSemaphores;
    private readonly Fence[] inFlightFences;
    private readonly Fence[] imagesInFlight;
    private int currentFrame = 0;

    private readonly string[] deviceExtensions = new[]
    {
            KhrSwapchain.ExtensionName
    };


    public Renderer(string appName)
    {
        _appName = appName;
        _api = new();
        _data._window = RenderHelper.CreateWindow(_api.sdl, _appName);
        _rendererData = new RendererData(_api, _data._window, deviceExtensions, _appName, RendererName);

        renderPass = RenderHelper.CreateRenderPass(_api, _rendererData.device, _rendererData.format.Format);
        int w, h;
        w = h = 0;
        _api.sdl.VulkanGetDrawableSize(_data._window, ref w, ref h);
        swapChain = new SwapChain(_api, _rendererData, renderPass, w, h);
        (graphicsPipeline, pipelineLayout) = RenderHelper.CreateGraphicsPipeline(_api, _rendererData.device, swapChain.extent, renderPass);
        commandPool = RenderHelper.CreateCommandPool(_api, _rendererData.instance, _rendererData.gpu, _rendererData.device, _rendererData.surface);
        commandBuffers = RenderHelper.CreateCommandBuffers(_api, swapChain.frameBuffers.AsSpan(), commandPool, _rendererData.device, renderPass, swapChain.extent, graphicsPipeline); ;
        (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences, imagesInFlight) = RenderHelper.CreateSyncObjects(_api, _rendererData.device, swapChain.images.Length, 1);
    }

    public void SetWindowPositionAndSize((int x, int y, int w, int h) rect)
    {
        _api.sdl.SetWindowPosition(_data._window, rect.x, rect.y);
        _api.sdl.SetWindowSize(_data._window, rect.w, rect.h);
    }

    public void Dispose()
    {
        foreach (var semaphore in renderFinishedSemaphores)
            _api.vk.DestroySemaphore(_rendererData.device, semaphore, null);
        foreach (var semaphore in imageAvailableSemaphores)
            _api.vk.DestroySemaphore(_rendererData.device, semaphore, null);
        foreach (var fence in inFlightFences)
            _api.vk.DestroyFence(_rendererData.device, fence, null);
        _api.vk.DestroyCommandPool(_rendererData.device, commandPool, null);

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

        _api.vk.DestroyDevice(_rendererData.device, null);
        _api.vk.DestroyInstance(_rendererData.instance, null);
        _api.vk.Dispose();
        _api.sdl.DestroyWindow(_data._window);
        _api.sdl.Dispose();
    }

    public void Run()
    {
        _api.sdl.PollEvent((Silk.NET.SDL.Event*)null);
    }
    public void Draw()
    {
        _ = _api.vk.TryGetDeviceExtension(_rendererData.instance, _rendererData.device, out KhrSwapchain khrSwapChain);
        RenderHelper.DrawFrame(_api, _rendererData.device, swapChain.swapChain, khrSwapChain, commandBuffers,
            inFlightFences, imagesInFlight, imageAvailableSemaphores, renderFinishedSemaphores,
            _rendererData.graphicsQueue, _rendererData.presentQueue, ref currentFrame);
    }




    public class RendererData
    {
        public Vk vk;
        public readonly Instance instance;
        public readonly SurfaceKHR surface;
        public readonly PhysicalDevice gpu;
        public readonly Device device;

        public readonly SurfaceFormatKHR format;

        public readonly SwapChainSupportDetails swapChainSupport;
        public readonly QueueFamilyIndiciesDetails indiciesDetails;

        public readonly Queue graphicsQueue;
        public readonly Queue presentQueue;

        public RendererData(Api api, Window* window, string[] deviceExtensions, string appName, string rendererName)
        {
            vk = api.vk;
            instance = RenderHelper.CreateVkInstance(vk, api.sdl, window, appName, rendererName);
            _ = vk.TryGetInstanceExtension<KhrSurface>(instance, out var khrsf);
            surface = RenderHelper.CreateSurface(api.sdl, window, instance);
            gpu = RenderHelper.PickPhysicalDevice(vk, instance, surface, khrsf, deviceExtensions);
            (device, graphicsQueue, presentQueue) = RenderHelper.CreateLogicalDevice(vk, gpu, surface, khrsf, deviceExtensions);

            swapChainSupport = new SwapChainSupportDetails(gpu, surface, khrsf);
            indiciesDetails = new QueueFamilyIndiciesDetails(vk, surface, gpu, khrsf);
            format = RenderHelper.ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        }
    }
}
