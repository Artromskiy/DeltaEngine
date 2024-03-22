using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Immutable;

namespace Delta.Rendering.Internal;

internal readonly struct SwapChainSupportDetails
{
    public readonly SurfaceCapabilitiesKHR Capabilities;
    public readonly ImmutableArray<PresentModeKHR> PresentModes;
    public readonly ImmutableArray<SurfaceFormatKHR> Formats;

    public unsafe SwapChainSupportDetails(PhysicalDevice physicalDevice, SurfaceKHR surface, KhrSurface khrsf)
    {
        _ = khrsf.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out Capabilities);
        (var formatCount, var presentModeCount) = GetSizes(physicalDevice, surface, khrsf);

        Span<SurfaceFormatKHR> formats = stackalloc SurfaceFormatKHR[(int)formatCount];
        _ = khrsf.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, formats);
        Formats = ImmutableArray.Create(formats);
        Span<PresentModeKHR> presentModes = stackalloc PresentModeKHR[(int)presentModeCount];
        _ = khrsf.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &presentModeCount, presentModes);
        PresentModes = ImmutableArray.Create(presentModes);
    }

    public static bool Adequate(PhysicalDevice physicalDevice, SurfaceKHR surface, KhrSurface khrsf)
    {
        var (formatCount, presentModeCount) = GetSizes(physicalDevice, surface, khrsf);
        return formatCount != 0 && presentModeCount != 0;
    }

    private static unsafe (uint formatCount, uint presentModeCount) GetSizes(PhysicalDevice physicalDevice, SurfaceKHR surface, KhrSurface khrsf)
    {
        uint formatCount = 0;
        _ = khrsf.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, null);

        uint presentModeCount = 0;
        _ = khrsf.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &presentModeCount, null);
        return (formatCount, presentModeCount);
    }
}

