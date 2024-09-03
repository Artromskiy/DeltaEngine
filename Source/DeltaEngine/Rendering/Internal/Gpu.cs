using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;

namespace Delta.Rendering.Internal;
internal class Gpu
{
    private readonly PhysicalDevice physicalDevice;
    private readonly QueueFamilyProperties[] queueFamilies;
    public readonly PhysicalDeviceMemoryProperties memoryProperties;

    public static implicit operator PhysicalDevice(Gpu gpu) => gpu.physicalDevice;

    public unsafe Gpu(Vk vk, PhysicalDevice physicalDevice)
    {
        this.physicalDevice = physicalDevice;
        uint queueFamilyCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, null);
        queueFamilies = new QueueFamilyProperties[(int)queueFamilyCount];
        vk.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, queueFamilies);
        memoryProperties = vk.GetPhysicalDeviceMemoryProperties(physicalDevice);
    }

    public bool HasQueue(QueueType graphicsQueueType)
    {
        var flag = graphicsQueueType switch
        {
            QueueType.Graphics => QueueFlags.GraphicsBit,
            QueueType.Transfer => QueueFlags.TransferBit,
            QueueType.Compute => QueueFlags.ComputeBit,
            _ => default
        };
        return Array.Exists(queueFamilies, qf => qf.QueueFlags.HasFlag(flag));
    }

    public bool SupportsPresent(SurfaceKHR surface, KhrSurface khrsf)
    {
        for (int i = 0; i < queueFamilies.Length; i++)
        {
            _ = khrsf.GetPhysicalDeviceSurfaceSupport(physicalDevice, (uint)i, surface, out var presentSupport);
            if (presentSupport)
                return true;
        }
        return false;
    }
    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        var typeFilterInt = (int)typeFilter;
        var memoryTypes = memoryProperties.MemoryTypes;
        for (int i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            bool indexMatch = (typeFilterInt & (1 << i)) != 0; // some mask magic
            bool flagsMatch = memoryTypes[i].PropertyFlags.HasFlag(properties);
            if (indexMatch && flagsMatch)
                return (uint)i;
        }
        _ = false;
        return 0;
    }


    public uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties, out MemoryPropertyFlags memoryFlagsHas)
    {
        var typeFilterInt = (int)typeFilter;
        var memoryTypes = memoryProperties.MemoryTypes;
        memoryFlagsHas = MemoryPropertyFlags.None;

        for (int i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            memoryFlagsHas = memoryTypes[i].PropertyFlags;
            bool indexMatch = ((typeFilterInt & (1 << i)) != 0); // some mask magic
            bool flagsMatch = memoryFlagsHas.HasFlag(properties);
            if (indexMatch && flagsMatch)
                return (uint)i;
        }
        _ = false;
        return 0;
    }
}