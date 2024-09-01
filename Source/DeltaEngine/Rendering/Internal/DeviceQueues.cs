using Delta.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering.Internal;
internal readonly struct DeviceQueues : IDisposable
{
    private readonly Vk _vk;

    private readonly Device device;
    /*
    public readonly Queue graphicsQueue;
    public readonly Queue presentQueue;
    public readonly Queue computeQueue;
    public readonly Queue transferQueue;

    public readonly CommandPool graphicsCmdPool;
    public readonly CommandPool presentCmdPool;
    public readonly CommandPool computeCmdPool;
    public readonly CommandPool transferCmdPool;
    */
    private readonly CommandPool?[] _cmdPools = new CommandPool?[Enums.GetCount<QueueType>()];
    private readonly Queue?[] _queues = new Queue?[Enums.GetCount<QueueType>()];

    public readonly FamilyQueues familyQueues;
    public readonly Gpu gpu;

    public static implicit operator Device(DeviceQueues deviceQueues) => deviceQueues.device;

    public Queue GetQueue(QueueType queueType) => _queues[(int)queueType]!.Value;
    public bool HasQueue(QueueType queueType) => _queues[(int)queueType].HasValue;
    public CommandPool GetCmdPool(QueueType queueType) => _cmdPools[(int)queueType]!.Value;
    public bool HasCmdPool(QueueType queueType) => _cmdPools[(int)queueType].HasValue;

    public unsafe DeviceQueues(Vk vk, Gpu gpu, FamilyQueues familyQueues, ReadOnlySpan<string> deviceExtensions)
    {
        _vk = vk;
        this.gpu = gpu;
        this.familyQueues = familyQueues;
        Span<(uint queueFamily, int count)> uniqueFamilyIndices = stackalloc (uint, int)[4];
        uniqueFamilyIndices = uniqueFamilyIndices[..familyQueues.GetUniqueFamilies(uniqueFamilyIndices)];
        var uniqueQueueFam = stackalloc DeviceQueueCreateInfo[uniqueFamilyIndices.Length];
        var queuePriority = stackalloc float[] { 1.0f, 1.0f, 1.0f, 1.0f };
        for (int i = 0; i < uniqueFamilyIndices.Length; i++)
        {
            uniqueQueueFam[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueFamilyIndices[i].queueFamily,
                QueueCount = (uint)uniqueFamilyIndices[i].count,
                PQueuePriorities = queuePriority
            };
        }
        PhysicalDeviceFeatures deviceFeatures = new();
        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueFamilyIndices.Length,
            PQueueCreateInfos = uniqueQueueFam,
            PEnabledFeatures = &deviceFeatures,
            EnabledExtensionCount = (uint)deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions.ToArray()),
            EnabledLayerCount = 0
        };
        _ = vk.CreateDevice(gpu, &createInfo, null, out device);

        Span<CommandPool> cmdPools = stackalloc CommandPool[uniqueFamilyIndices.Length];
        for (int i = 0; i < cmdPools.Length; i++)
        {
            var cmdPoolInfo = new CommandPoolCreateInfo()
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
                QueueFamilyIndex = uniqueFamilyIndices[i].queueFamily,
            };
            vk.CreateCommandPool(device, cmdPoolInfo, null, out cmdPools[i]);
        }

        var queueTypes = Enums.GetValues<QueueType>();
        int length = queueTypes.Length;
        for (int i = 0; i < length; i++)
        {
            if (familyQueues.HasQueue(queueTypes[i]))
            {
                var (family, queueNum) = familyQueues[queueTypes[i]];
                _queues[i] = vk.GetDeviceQueue(device, family, queueNum);
                _cmdPools[i] = cmdPools[uniqueFamilyIndices.FindIndex(x => x.queueFamily == queueNum)];
            }
        }
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
    }

    public unsafe void Dispose()
    {
        for (int i = 0; i < _cmdPools.Length; i++)
        {
            if (_cmdPools[i].HasValue)
                _vk.DestroyCommandPool(device, _cmdPools[i]!.Value, null);
        }
        _vk.DestroyDevice(device, null);
    }
}
