using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace DeltaEngine.Rendering;

public class RenderBase
{
    public Vk vk;
    public readonly Instance instance;
    public readonly SurfaceKHR surface;
    public readonly PhysicalDevice gpu;
    public readonly Device device;

    public readonly KhrSurface khrsf;

    public readonly SurfaceFormatKHR format;

    public SwapChainSupportDetails swapChainSupport;
    public readonly QueueFamilyIndiciesDetails indiciesDetails;

    public readonly Queue graphicsQueue;
    public readonly Queue presentQueue;

    public unsafe RenderBase(Api api, Window* window, string[] deviceExtensions, string appName, string rendererName)
    {
        vk = api.vk;
        instance = RenderHelper.CreateVkInstance(vk, api.sdl, window, appName, rendererName);
        _ = vk.TryGetInstanceExtension(instance, out khrsf);
        surface = RenderHelper.CreateSurface(api.sdl, window, instance);
        gpu = RenderHelper.PickPhysicalDevice(vk, instance, surface, khrsf, deviceExtensions);
        (device, graphicsQueue, presentQueue) = RenderHelper.CreateLogicalDevice(vk, gpu, surface, khrsf, deviceExtensions);

        swapChainSupport = new SwapChainSupportDetails(gpu, surface, khrsf);
        indiciesDetails = new QueueFamilyIndiciesDetails(vk, surface, gpu, khrsf);
        format = RenderHelper.ChooseSwapSurfaceFormat(swapChainSupport.Formats);
    }

    public void UpdateSupportDetails()
    {
        swapChainSupport = new SwapChainSupportDetails(gpu, surface, khrsf);
    }
}