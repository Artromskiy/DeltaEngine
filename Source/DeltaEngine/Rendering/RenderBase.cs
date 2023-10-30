﻿using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Runtime.InteropServices;

namespace DeltaEngine.Rendering;

public class RenderBase : IDisposable
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
    public readonly MemoryDetails gpuMemory;

    public readonly Queue graphicsQueue;
    public readonly Queue presentQueue;

    public readonly CommandPool commandPool;

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
        var layers = validationSupported ? validationLayers : Array.Empty<string>();
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

        (device, graphicsQueue, presentQueue) = RenderHelper.CreateLogicalDevice(vk, gpu, surface, khrsf, deviceExtensions);

        swapChainSupport = new SwapChainSupportDetails(gpu, surface, khrsf);
        indiciesDetails = new QueueFamilyIndiciesDetails(vk, surface, gpu, khrsf);
        format = RenderHelper.ChooseSwapSurfaceFormat(swapChainSupport.Formats, targetFormat);
        commandPool = RenderHelper.CreateCommandPool(this);
    }

    public unsafe void Dispose()
    {
        vk.DestroyCommandPool(device, commandPool, null);
        vk.DestroyDevice(device, null);
        khrsf.DestroySurface(instance, surface, null);
        vk.DestroyInstance(instance, null);
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