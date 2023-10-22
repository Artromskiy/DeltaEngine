using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Immutable;

namespace DeltaEngine.Rendering;

public readonly struct QueueFamilyIndiciesDetails
{
    public readonly ImmutableArray<QueueFamilyProperties> queueFamilyProperties;

    public readonly uint graphicsFamily;
    public readonly uint presentFamily;

    public readonly bool suitable;

    public unsafe QueueFamilyIndiciesDetails(Vk vk, SurfaceKHR surface, PhysicalDevice gpu, KhrSurface khrsf)
    {
        uint queueFamilityCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilityCount, null);
        Span<QueueFamilyProperties> queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilityCount];
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilityCount, queueFamilies);
        queueFamilyProperties = ImmutableArray.Create(queueFamilies);

        int graphicsFamily, presentFamily;
        graphicsFamily = presentFamily = -1;

        for (int i = 0; i < queueFamilyProperties.Length; i++)
        {
            if (queueFamilyProperties[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                graphicsFamily = i;

            _ = khrsf.GetPhysicalDeviceSurfaceSupport(gpu, (uint)i, surface, out var presentSupport);
            if (presentSupport)
                presentFamily = i;

            if (graphicsFamily >= 0 && presentFamily >= 0)
                break;
        }

        suitable = graphicsFamily >= 0 && presentFamily >= 0;
        this.graphicsFamily = (uint)graphicsFamily;
        this.presentFamily = (uint)presentFamily;
    }
}


