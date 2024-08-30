using Collections.Pooled;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;

namespace Delta.Rendering.Internal;

internal readonly struct QueueFamilyIndiciesDetails
{
    public readonly uint graphicsFamily;
    public readonly uint presentFamily;
    public readonly uint computeFamily;
    public readonly uint transferFamily;

    public readonly uint graphicsQueueNum;
    public readonly uint presentQueueNum;
    public readonly uint computeQueueNum;
    public readonly uint transferQueueNum;

    public readonly (uint family, uint queueNum) graphics => (graphicsFamily, graphicsQueueNum);
    public readonly (uint family, uint queueNum) present => (presentFamily, presentQueueNum);
    public readonly (uint family, uint queueNum) compute => (computeFamily, computeQueueNum);
    public readonly (uint family, uint queueNum) transfer => (transferFamily, transferQueueNum);

    public readonly bool suitable;

    public unsafe QueueFamilyIndiciesDetails(Vk vk, SurfaceKHR surface, PhysicalDevice gpu, KhrSurface khrsf)
    {
        uint queueFamilyCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, null);
        Span<QueueFamilyProperties> queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, queueFamilies);

        int graphicsIndex, presentIndex, transferIndex, computendex;
        graphicsIndex = presentIndex = transferIndex = computendex = -1;

        Span<uint> bookedFamilies = stackalloc uint[(int)queueFamilyCount];

        // Selection of specialized queues
        for (int i = 0; i < queueFamilies.Length; i++)
        {
            var props = queueFamilies[i];
            var flags = GetFlags(queueFamilies, i, gpu, surface, khrsf);

            if (bookedFamilies[i] < props.QueueCount && flags.hasGraphics && graphicsIndex < 0)
            {
                graphicsIndex = i;
                graphicsQueueNum = bookedFamilies[i]++;
            }
            if (bookedFamilies[i] < props.QueueCount && i != graphicsIndex && flags.hasPresent && presentIndex < 0)
            {
                presentIndex = i;
                presentQueueNum = bookedFamilies[i]++;
            }
            if (bookedFamilies[i] < props.QueueCount && !flags.hasGraphics && flags.hasCompute && computendex < 0)
            {
                computendex = i;
                computeQueueNum = bookedFamilies[i]++;
            }
            if (bookedFamilies[i] < props.QueueCount && !flags.hasGraphics && flags.hasTransfer && transferIndex < 0)
            {
                transferIndex = i;
                transferQueueNum = bookedFamilies[i]++;
            }
            if (graphicsIndex >= 0 && presentIndex >= 0 && computendex >= 0 && transferIndex >= 0)
                break;
        }

        // fallback if no unique specialized queue search just specialized
        for (int i = 0; i < queueFamilies.Length; i++)
        {
            if (graphicsIndex >= 0 && presentIndex >= 0 && computendex >= 0 && transferIndex >= 0)
                break;

            var flags = GetFlags(queueFamilies, i, gpu, surface, khrsf);

            if (flags.hasPresent && presentIndex < 0)
            {
                presentIndex = i;
                presentQueueNum = 0;
            }
            if (!flags.hasGraphics && flags.hasCompute && computendex < 0)
            {
                computendex = i;
                computeQueueNum = 0;
            }
            if (!flags.hasGraphics && flags.hasTransfer && transferIndex < 0)
            {
                transferIndex = i;
                transferQueueNum = 0;
            }
        }

        // fallback to anything supported
        for (int i = 0; i < queueFamilies.Length; i++)
        {
            if (graphicsIndex >= 0 && presentIndex >= 0 && computendex >= 0 && transferIndex >= 0)
                break;

            var flags = GetFlags(queueFamilies, i, gpu, surface, khrsf);

            if (flags.hasCompute && computendex < 0)
            {
                computendex = i;
                computeQueueNum = 0;
            }
            if (flags.hasTransfer && transferIndex < 0)
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

    private static QueueFlagsData GetFlags(Span<QueueFamilyProperties> queueProps, int index, PhysicalDevice gpu, SurfaceKHR surface, KhrSurface khrsf)
    {
        var props = queueProps[index];
        var flags = props.QueueFlags;
        _ = khrsf.GetPhysicalDeviceSurfaceSupport(gpu, (uint)index, surface, out var presentSupport);
        return new QueueFlagsData(
            flags.HasFlag(QueueFlags.GraphicsBit),
            flags.HasFlag(QueueFlags.TransferBit),
            flags.HasFlag(QueueFlags.ComputeBit),
            presentSupport);
    }

    private readonly struct QueueFlagsData
    {
        public readonly bool hasGraphics;
        public readonly bool hasTransfer;
        public readonly bool hasCompute;
        public readonly bool hasPresent;

        public QueueFlagsData(bool hasGraphics, bool hasTransfer, bool hasCompute, bool hasPresent)
        {
            this.hasGraphics = hasGraphics;
            this.hasTransfer = hasTransfer;
            this.hasCompute = hasCompute;
            this.hasPresent = hasPresent;
        }
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


