using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Text;
using static DeltaEngine.ThrowHelper;

namespace DeltaEngine
{
    public struct Api
    {
        public Sdl sdl;
        public Vk vk;
        public KhrSurface _khrSurf;
    }

    internal sealed unsafe class Renderer : IDisposable
    {
        private readonly Data _data;
        private Api _api;


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
        private readonly byte[] name = Encoding.UTF8.GetBytes(RendererName);
        private readonly string _appName;

        private readonly string[] requiredVkLayers = new[]
        {
            "VK_LAYER_NV_optimus",
            "VK_LAYER_KHRONOS_validation"
        };
        private readonly string[] deviceExtensions = new[]
        {
            KhrSwapchain.ExtensionName
        };


        public Renderer(string appName)
        {
            _appName = appName;
            _api = RenderHelper.CreateApi();
            _data._window = RenderHelper.CreateWindow(_api, RendererName);
            //_api.sdl.SetWindowResizable(_data._window, SdlBool.False);
            //_api.sdl.SetWindowBordered(_data._window, SdlBool.False);
            var extens = RenderHelper.GetVulkanExtensions(_api, _data._window);
            var layers = RenderHelper.GetVulkanLayers(_api, requiredVkLayers);
            _data._instance = RenderHelper.CreateVkInstance(_api, _data._window, appName, RendererName);
            _ = _api.vk.TryGetInstanceExtension<KhrSurface>(_data._instance, out var khrsf);
            _data._surface = RenderHelper.CreateSurface(_api, _data._window, _data._instance);
            //_ = layers.Length == reqVkLayers.Length;
            _data._gpu = RenderHelper.PickPhysicalDevice(_api, _data._instance, _data._surface, khrsf, deviceExtensions);
            (_data._device, var graphicsQueue, var presentQueue) = RenderHelper.CreateLogicalDevice(_api, _data._gpu, _data._surface, khrsf, deviceExtensions);
            (var khrSwapChain, _data._swapchain, var swapChainImages, var swapChainImageFormat, var swapChainExtent) = RenderHelper.CreateSwapChain(_api, _data._window, _data._instance, _data._device, _data._gpu, _data._surface, khrsf);
            var swapChainImageViews = RenderHelper.CreateImageViews(_api, _data._device, swapChainImages, swapChainImageFormat);
            var renderPass = RenderHelper.CreateRenderPass(_api, _data._device, swapChainImageFormat);

            //_data._device = CreateDevice(_api, _data._gpu);
            //_ = _api.vk.TryGetInstanceExtension(_data._instance, out _api._khrSurf);
            //PresentModeKHR pm = PresentModeKHR.FifoKhr;
            //_data._swapchain = CreateSwapChain(_api, _data, gsurfaceFormat, gTransform, requestedImageUsages, ref pm);
            //var ih = getSwapChainImageHandles(_device, _swapchain);
            //vk.GetDeviceQueue(_device, 0, 0, out _queue);
        }

        public void Dispose()
        {
            new KhrSwapchain(_api.vk.Context).DestroySwapchain(_data._device, _data._swapchain, null);
            _api.vk.DestroyDevice(_data._device, null);
            _api.vk.DestroyInstance(_data._instance, null);
            _api.vk.Dispose();
            _api.sdl.DestroyWindow(_data._window);
            _api.sdl.Dispose();
        }

        public void Run()
        {
            _api.sdl.PollEvent((Silk.NET.SDL.Event*)null);
            //sdl.SetWindowResizable(_window, SdlBool.False);
            //sdl.SetWindowBordered(_window, SdlBool.False);
            //int t, l, b, r;
            //t = l = b = r = 0;
            //sdl.GetWindowBordersSize(_window, ref t, ref l, ref b, ref r);
            //Console.WriteLine("s");
        }

    }
}
