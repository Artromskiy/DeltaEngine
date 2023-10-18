using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using static DeltaEngine.ThrowHelper;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace DeltaEngine;

internal sealed unsafe class Renderer : IDisposable
{
    private readonly Data _data;
    private readonly Api _api;


    private struct Data
    {
        public Window* _window;
        public Instance _instance;
        public PhysicalDevice _gpu;
        public Device _device;
        public SurfaceKHR _surface;
        public SwapchainKHR _swapchain;
        public Queue _queue;
    }

    private const string RendererName = "Delta Renderer";
    private readonly string _appName;

    private Pipeline? graphicsPipeline;

    private Queue graphicsQueue;
    private Queue presentQueue;

    private readonly KhrSwapchain khrSwapChain;
    private readonly KhrSurface khrsf;

    private RenderPass renderPass;
    private PipelineLayout pipelineLayout;
    private CommandPool commandPool;
    private readonly Image[] swapChainImages;
    private readonly ImageView[] swapChainImageViews;
    private readonly Framebuffer[] swapChainFramebuffers;
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
        _data._window = RenderHelper.CreateWindow(_api, _appName);
        _data._instance = RenderHelper.CreateVkInstance(_api, _data._window, _appName, RendererName);
        _ = _api.vk.TryGetInstanceExtension(_data._instance, out khrsf);
        _data._surface = RenderHelper.CreateSurface(_api, _data._window, _data._instance);
        _data._gpu = RenderHelper.PickPhysicalDevice(_api, _data._instance, _data._surface, khrsf, deviceExtensions);
        (_data._device, graphicsQueue, presentQueue) = RenderHelper.CreateLogicalDevice(_api, _data._gpu, _data._surface, khrsf, deviceExtensions);
        (khrSwapChain, _data._swapchain, swapChainImages, var swapChainImageFormat, var swapChainExtent) = RenderHelper.CreateSwapChain(_api, _data._window, _data._instance, _data._device, _data._gpu, _data._surface, khrsf);
        swapChainImageViews = RenderHelper.CreateImageViews(_api, _data._device, swapChainImages, swapChainImageFormat);
        renderPass = RenderHelper.CreateRenderPass(_api, _data._device, swapChainImageFormat);
        //(graphicsPipeline, pipelineLayout) = RenderHelper.CreateGraphicsPipeline(_api, device, swapChainExtent, renderPass);
        swapChainFramebuffers = RenderHelper.CreateFramebuffers(_api, _data._device, swapChainImageViews, renderPass, swapChainExtent);
        commandPool = RenderHelper.CreateCommandPool(_api, _data._gpu, _data._device, _data._surface, khrsf);
        commandBuffers = RenderHelper.CreateCommandBuffers(_api, swapChainFramebuffers, commandPool, _data._device, renderPass, swapChainExtent, graphicsPipeline);
        (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences, imagesInFlight) = RenderHelper.CreateSyncObjects(_api, _data._device, swapChainImages, 1);
    }

    public void Dispose()
    {
        foreach (var semaphore in renderFinishedSemaphores)
            _api.vk.DestroySemaphore(_data._device, semaphore, null);
        foreach (var semaphore in imageAvailableSemaphores)
            _api.vk.DestroySemaphore(_data._device, semaphore, null);
        foreach (var fence in inFlightFences)
            _api.vk.DestroyFence(_data._device, fence, null);
        _api.vk.DestroyCommandPool(_data._device, commandPool, null);

        foreach (var framebuffer in swapChainFramebuffers!)
            _api.vk.DestroyFramebuffer(_data._device, framebuffer, null);

        if (graphicsPipeline.HasValue)
            _api.vk.DestroyPipeline(_data._device, graphicsPipeline.Value, null);

        _api.vk.DestroyPipelineLayout(_data._device, pipelineLayout, null);
        _api.vk.DestroyRenderPass(_data._device, renderPass, null);

        foreach (var image in swapChainImages)
            _api.vk.DestroyImage(_data._device, image, null);
        foreach (var imageView in swapChainImageViews)
            _api.vk.DestroyImageView(_data._device, imageView, null);

        khrSwapChain.DestroySwapchain(_data._device, _data._swapchain, null);

        _api.vk.DestroyDevice(_data._device, null);

        khrsf.DestroySurface(_data._instance, _data._surface, null);
        _api.vk.DestroyInstance(_data._instance, null);
        _api.vk.Dispose();

        _api.vk.DestroyDevice(_data._device, null);
        _api.vk.DestroyInstance(_data._instance, null);
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
        RenderHelper.DrawFrame(_api, _data._device, _data._swapchain, khrSwapChain, commandBuffers, inFlightFences, imagesInFlight, imageAvailableSemaphores, renderFinishedSemaphores, graphicsQueue, presentQueue, ref currentFrame);
    }
}
