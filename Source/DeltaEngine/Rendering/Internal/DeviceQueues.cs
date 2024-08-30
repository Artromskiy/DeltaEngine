using Delta.Utilities;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering.Internal;
internal readonly struct DeviceQueues : IDisposable
{
    private readonly Vk _vk;

    private readonly Device device;

    public readonly Queue graphicsQueue;
    public readonly Queue presentQueue;
    public readonly Queue computeQueue;
    public readonly Queue transferQueue;

    public readonly CommandPool graphicsCmdPool;
    public readonly CommandPool presentCmdPool;
    public readonly CommandPool computeCmdPool;
    public readonly CommandPool transferCmdPool;

    public readonly QueueFamilies queueFamilies;
    public readonly Gpu gpu;

    public static implicit operator Device(DeviceQueues deviceQueues) => deviceQueues.device;

    public unsafe DeviceQueues(Vk vk, Gpu gpu, QueueFamilies queueFamilies, ReadOnlySpan<string> deviceExtensions)
    {
        _vk = vk;
        this.gpu = gpu;
        this.queueFamilies = queueFamilies;
        Span<(uint queueFamily, uint count)> uniqueFamilyIndices = stackalloc (uint, uint)[4];
        uniqueFamilyIndices = uniqueFamilyIndices[..queueFamilies.GetUniqueFamilies(uniqueFamilyIndices)];
        var uniqueQueueFam = stackalloc DeviceQueueCreateInfo[uniqueFamilyIndices.Length];
        var queuePriority = stackalloc float[] { 1.0f, 1.0f, 1.0f, 1.0f };
        for (int i = 0; i < uniqueFamilyIndices.Length; i++)
        {
            uniqueQueueFam[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueFamilyIndices[i].queueFamily,
                QueueCount = uniqueFamilyIndices[i].count,
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

        graphicsQueue = vk.GetDeviceQueue(device, queueFamilies.graphics.family, queueFamilies.graphics.queueNum);
        presentQueue = vk.GetDeviceQueue(device, queueFamilies.present.family, queueFamilies.present.queueNum);
        computeQueue = vk.GetDeviceQueue(device, queueFamilies.compute.family, queueFamilies.compute.queueNum);
        transferQueue = vk.GetDeviceQueue(device, queueFamilies.transfer.family, queueFamilies.transfer.queueNum);

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

        graphicsCmdPool = cmdPools[uniqueFamilyIndices.FindIndex(x => x.queueFamily == queueFamilies.graphics.family)];
        presentCmdPool = cmdPools[uniqueFamilyIndices.FindIndex(x => x.queueFamily == queueFamilies.present.family)];
        computeCmdPool = cmdPools[uniqueFamilyIndices.FindIndex(x => x.queueFamily == queueFamilies.compute.family)];
        transferCmdPool = cmdPools[uniqueFamilyIndices.FindIndex(x => x.queueFamily == queueFamilies.transfer.family)];

        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
    }

    public unsafe void Dispose()
    {
        _vk.DestroyCommandPool(device, graphicsCmdPool, null);
        _vk.DestroyCommandPool(device, presentCmdPool, null);
        _vk.DestroyCommandPool(device, computeCmdPool, null);
        _vk.DestroyCommandPool(device, transferCmdPool, null);
        _vk.DestroyDevice(device, null);
    }
}
