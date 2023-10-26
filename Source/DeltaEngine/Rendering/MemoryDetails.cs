using Silk.NET.Vulkan;

namespace DeltaEngine.Rendering;
public readonly struct MemoryDetails
{
    public readonly PhysicalDeviceMemoryProperties memoryProperties;

    public MemoryDetails(Vk vk, PhysicalDevice gpu)
    {
        memoryProperties = vk.GetPhysicalDeviceMemoryProperties(gpu);
    }
}
