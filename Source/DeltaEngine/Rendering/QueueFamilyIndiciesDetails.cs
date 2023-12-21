using Collections.Pooled;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Immutable;

namespace Delta.Rendering;

public readonly struct QueueFamilyIndiciesDetails
{
    public readonly ImmutableArray<QueueFamilyProperties> queueFamilyProperties;

    public readonly uint graphicsFamily;
    public readonly uint presentFamily;
    public readonly uint computeFamily;
    public readonly uint transferFamily;

    public readonly uint graphicsQueueNum;
    public readonly uint presentQueueNum;
    public readonly uint computeQueueNum;
    public readonly uint transferQueueNum;

    public readonly bool suitable;

    public unsafe QueueFamilyIndiciesDetails(Vk vk, SurfaceKHR surface, PhysicalDevice gpu, KhrSurface khrsf)
    {
        uint queueFamilityCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilityCount, null);
        Span<QueueFamilyProperties> queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilityCount];
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilityCount, queueFamilies);
        queueFamilyProperties = ImmutableArray.Create(queueFamilies);

        int graphicsIndex, presentIndex;
        graphicsIndex = presentIndex = -1;
        int transferIndex = -1;
        int computendex = -1;

        Span<uint> bookedFamilies = stackalloc uint[(int)queueFamilityCount];

        // Selection of specialized queues
        for (int i = 0; i < queueFamilyProperties.Length; i++)
        {
            var props = queueFamilyProperties[i];
            var flags = props.QueueFlags;
            _ = khrsf.GetPhysicalDeviceSurfaceSupport(gpu, (uint)i, surface, out var presentSupport);

            bool hasGraphics = flags.HasFlag(QueueFlags.GraphicsBit);
            bool hasTransfer = flags.HasFlag(QueueFlags.TransferBit);
            bool hasCompute = flags.HasFlag(QueueFlags.ComputeBit);
            bool hasPresent = presentSupport;

            if (bookedFamilies[i] < props.QueueCount && hasGraphics && graphicsIndex < 0)
            {
                graphicsIndex = i;
                graphicsQueueNum = bookedFamilies[i]++;
            }
            if (bookedFamilies[i] < props.QueueCount && i != graphicsIndex && presentSupport && presentIndex < 0)
            {
                presentIndex = i;
                presentQueueNum = bookedFamilies[i]++;
            }
            if (bookedFamilies[i] < props.QueueCount && !hasGraphics && hasCompute && computendex < 0)
            {
                computendex = i;
                computeQueueNum = bookedFamilies[i]++;
            }
            if (bookedFamilies[i] < props.QueueCount && !hasGraphics && hasTransfer && transferIndex < 0)
            {
                transferIndex = i;
                transferQueueNum = bookedFamilies[i]++;
            }
            if (graphicsIndex >= 0 && presentIndex >= 0 && computendex >= 0 && transferIndex >= 0)
                break;
        }

        // fallback if no unique specialized queue search just specialized
        for (int i = 0; i < queueFamilyProperties.Length; i++)
        {
            if (graphicsIndex >= 0 && presentIndex >= 0 && computendex >= 0 && transferIndex >= 0)
                break;
            var props = queueFamilyProperties[i];
            var flags = props.QueueFlags;
            _ = khrsf.GetPhysicalDeviceSurfaceSupport(gpu, (uint)i, surface, out var presentSupport);

            bool hasGraphics = flags.HasFlag(QueueFlags.GraphicsBit);
            bool hasTransfer = flags.HasFlag(QueueFlags.TransferBit);
            bool hasCompute = flags.HasFlag(QueueFlags.ComputeBit);
            bool hasPresent = presentSupport;

            if (presentSupport && presentIndex < 0)
            {
                presentIndex = i;
                presentQueueNum = 0;
            }
            if (!hasGraphics && hasCompute && computendex < 0)
            {
                computendex = i;
                computeQueueNum = 0;
            }
            if (!hasGraphics && hasTransfer && transferIndex < 0)
            {
                transferIndex = i;
                transferQueueNum = 0;
            }
        }

        // fallback to anything supported
        for (int i = 0; i < queueFamilyProperties.Length; i++)
        {
            if (graphicsIndex >= 0 && presentIndex >= 0 && computendex >= 0 && transferIndex >= 0)
                break;
            var props = queueFamilyProperties[i];
            var flags = props.QueueFlags;

            bool hasTransfer = flags.HasFlag(QueueFlags.TransferBit);
            bool hasCompute = flags.HasFlag(QueueFlags.ComputeBit);

            if (hasCompute && computendex < 0)
            {
                computendex = i;
                computeQueueNum = 0;
            }
            if (hasTransfer && transferIndex < 0)
            {
                transferIndex = i;
                transferQueueNum = 0;
            }
        }


        suitable = graphicsIndex >= 0 && presentIndex >= 0;

        graphicsFamily = (uint)graphicsIndex;
        presentFamily = (uint)presentIndex;
        computeFamily = (uint)computendex;
        transferFamily = (uint)transferIndex;

        var c = GetUniqueCount();
    }

    public int GetUniqueFamilies(Span<(uint family, uint num)> uniqueFamilies)
    {
        PooledSet<uint> families = [graphicsFamily, presentFamily, computeFamily, transferFamily];
        int i = 0;
        foreach (var item in families)
        {
            uint gr = item == graphicsFamily && graphicsQueueNum > 0 ? 1u : 0u;
            uint pr = item == presentFamily && presentQueueNum > 0 ? 1u : 0u;
            uint cm = item == computeFamily && computeQueueNum > 0 ? 1u : 0u;
            uint tr = item == transferFamily && transferQueueNum > 0 ? 1u : 0u;
            uniqueFamilies[i] = (item, 1 + gr + pr + cm + tr);
            i++;
        }
        var count = families.Count;
        families.Dispose();
        return count;
    }

    public int GetUniqueCount()
    {
        Span<uint> families = [graphicsFamily, presentFamily, computeFamily, transferFamily];
        Span<uint> uniqueFamilies = stackalloc uint[4];
        int count = 0;
        for (int i = 0; i < families.Length; i++)
        {
            var current = families[i];
            bool unique = true;
            for (int j = 0; j < count; j++)
            {
                if (current == uniqueFamilies[j])
                {
                    unique = false;
                    break;
                }
            }
            if (unique)
                uniqueFamilies[count++] = current;
        }
        return count;
    }
}


