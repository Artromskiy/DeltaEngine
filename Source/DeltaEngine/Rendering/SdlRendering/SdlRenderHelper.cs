using Delta.Rendering.Internal;
using Delta.Utilities;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;

namespace Delta.Rendering.SdlRendering;
internal static unsafe class SdlRenderHelper
{
    internal static DeviceQueues CreateLogicalDevice(Vk vk, Gpu gpu, SurfaceKHR surface, KhrSurface khrsf, ReadOnlySpan<string> deviceExtensions)
    {
        var queueFamilies = SelectQueueFamilies(vk, gpu, surface, khrsf);
        return new DeviceQueues(vk, gpu, queueFamilies, deviceExtensions);
    }

    public static FamilyQueues SelectQueueFamilies(Vk vk, Gpu gpu, SurfaceKHR surface, KhrSurface khrsf)
    {
        uint queueFamilyCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, null);
        Span<QueueFamilyProperties> queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, queueFamilies);
        int length = (int)queueFamilyCount;
        Span<int> maxQueuesCount = stackalloc int[length];
        Span<int> queuesCount = stackalloc int[length];
        Span<QueueFlagsData> supportFlags = new QueueFlagsData[length];
        var values = Enums.GetValues<QueueType>();
        for (int i = 0; i < length; i++)
        {
            var props = queueFamilies[i];
            maxQueuesCount[i] = (int)props.QueueCount;
            supportFlags[i] = GetFlags(queueFamilies, i, gpu, surface, khrsf);
        }
        Span<(int family, int queueNum)?> selected = stackalloc (int, int)?[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < length; j++)
            {
                var supported = supportFlags[j][values[i]];
                bool empty = queuesCount[j] == 0;
                bool hasFreeSpace = maxQueuesCount[j] > queuesCount[j];
                if (supported && empty && hasFreeSpace)
                {
                    selected[i] = (j, queuesCount[j]++);
                    goto end;
                }
            }

            for (int j = 0; j < length; j++)
            {
                var supported = supportFlags[j][values[i]];
                bool hasFreeSpace = maxQueuesCount[j] > queuesCount[j];
                if (supported && hasFreeSpace)
                {
                    selected[i] = (j, queuesCount[j]++);
                    goto end;
                }
            }
        end:;
        }
        return new FamilyQueues(selected);
    }

    private static QueueFlagsData GetFlags(Span<QueueFamilyProperties> queueProps, int index, PhysicalDevice gpu, SurfaceKHR surface, KhrSurface khrsf)
    {
        var props = queueProps[index];
        var flags = props.QueueFlags;
        _ = khrsf.GetPhysicalDeviceSurfaceSupport(gpu, (uint)index, surface, out var presentSupport);
        return new QueueFlagsData()
        {
            [QueueType.Graphics] = flags.Supports(QueueType.Graphics),
            [QueueType.Transfer] = flags.Supports(QueueType.Transfer),
            [QueueType.Compute] = flags.Supports(QueueType.Compute),
            [QueueType.Present] = presentSupport,
        };
    }

    private readonly struct QueueFlagsData
    {
        private readonly bool[] _queues = new bool[Enums.GetCount<QueueType>()];
        public bool this[QueueType queueType]
        {
            get => _queues[(int)queueType];
            init => _queues[(int)queueType] = value;
        }
        public QueueFlagsData() { }
    }
}
