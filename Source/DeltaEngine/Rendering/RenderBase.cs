using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Runtime.InteropServices;

namespace DeltaEngine.Rendering;

public sealed class RenderBase : IDisposable
{
    public readonly Vk vk;
    public readonly Instance instance;
    public readonly SurfaceKHR surface;
    public readonly PhysicalDevice gpu;

    public readonly KhrSurface khrsf;

    public readonly SurfaceFormatKHR format;

    public SwapChainSupportDetails swapChainSupport;
    public readonly MemoryDetails gpuMemory;

    public readonly DeviceQueues deviceQ;

    public readonly DescriptorPool descriptorPool;

    private static readonly string[] validationLayers = new[]
    {
        "VK_LAYER_KHRONOS_validation"
    };

    public unsafe RenderBase(Api api, Window* window, string[] deviceExtensions, string appName, string rendererName, SurfaceFormatKHR targetFormat)
    {
        vk = api.vk;

        bool validationSupported = CheckValidationLayerSupport();
        bool debugUtilsSupported = vk.IsInstanceExtensionPresent(ExtDebugUtils.ExtensionName);
        var sdlExtensions = RenderHelper.GetRequiredVulkanExtensions(api.sdl, window);
        var layers = validationSupported ? validationLayers : [];
        var instanceExtensions = new string[debugUtilsSupported ? sdlExtensions.Length + 1 : sdlExtensions.Length];
        Array.Copy(sdlExtensions, instanceExtensions, sdlExtensions.Length);
        instanceExtensions[^1] = debugUtilsSupported ? ExtDebugUtils.ExtensionName : instanceExtensions[^1];

        DebugUtilsMessengerCreateInfoEXT debugCreateInfo = default;
        if (debugUtilsSupported)
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);

        instance = RenderHelper.CreateVkInstance(vk, appName, rendererName, instanceExtensions, layers, debugUtilsSupported ? &debugCreateInfo : null);
        _ = vk.TryGetInstanceExtension(instance, out khrsf);

        surface = RenderHelper.CreateSurface(api.sdl, window, instance);
        gpu = RenderHelper.PickPhysicalDevice(vk, instance, surface, khrsf, deviceExtensions);

        gpuMemory = new MemoryDetails(vk, gpu);

        deviceQ = RenderHelper.CreateLogicalDevice(vk, gpu, surface, khrsf, deviceExtensions);

        swapChainSupport = new SwapChainSupportDetails(gpu, surface, khrsf);

        format = RenderHelper.ChooseSwapSurfaceFormat(swapChainSupport.Formats, targetFormat);

        descriptorPool = RenderHelper.CreateDescriptorPool(this);
    }

    public unsafe void Dispose()
    {
        vk.DestroyDevice(deviceQ.device, null);
        khrsf.DestroySurface(instance, surface, null);
        vk.DestroyInstance(instance, null);
        vk.Dispose();
    }

    public void UpdateSupportDetails()
    {
        swapChainSupport = new SwapChainSupportDetails(gpu, surface, khrsf);
    }


    private unsafe bool CheckValidationLayerSupport()
    {
        uint layerCount = 0;
        vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        Span<LayerProperties> availableLayers = stackalloc LayerProperties[(int)layerCount];
        vk.EnumerateInstanceLayerProperties(&layerCount, availableLayers);
        foreach (var item in validationLayers)
            if (!availableLayers.Exist(layer => Marshal.PtrToStringAnsi((nint)layer.LayerName) == item))
                return false;
        return true;
    }

    private unsafe void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(DebugCallback);
    }

    private unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        var message = (nint)pCallbackData->PMessage;
        Console.WriteLine(Marshal.PtrToStringAnsi(message));
        return Vk.True;
    }
}