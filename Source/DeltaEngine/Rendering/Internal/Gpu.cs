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
    public uint FindMemoryType(int typeFilter, MemoryPropertyFlags properties)
    {
        int i = 0;
        for (; i < memoryProperties.MemoryTypeCount; i++)
            if (Convert.ToBoolean(typeFilter & (1 << i)) && (memoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties) // some mask magic
                return (uint)i;
        _ = false;
        return (uint)i;
    }
    public uint FindMemoryType(int typeFilter, MemoryPropertyFlags properties, out MemoryPropertyFlags memoryFlagsHas)
    {
        memoryFlagsHas = MemoryPropertyFlags.None;
        int i = 0;
        for (; i < memoryProperties.MemoryTypeCount; i++)
            if (Convert.ToBoolean(typeFilter & (1 << i)) && (memoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties) // some mask magic
            {
                memoryFlagsHas = memoryProperties.MemoryTypes[i].PropertyFlags;
                return (uint)i;
            }
        _ = false;
        return (uint)i;
    }
}