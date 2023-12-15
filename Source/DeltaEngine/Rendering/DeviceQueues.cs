using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using System;

namespace DeltaEngine.Rendering;
public readonly struct DeviceQueues
{
    public readonly Device device;

    public readonly Queue graphicsQueue;
    public readonly Queue presentQueue;
    public readonly Queue computeQueue;
    public readonly Queue transferQueue;

    public readonly CommandPool graphicsCmdPool;
    public readonly CommandPool presentCmdPool;
    public readonly CommandPool computeCmdPool;
    public readonly CommandPool transferCmdPool;

    public readonly QueueFamilyIndiciesDetails queueIndicesDetails;

    public unsafe DeviceQueues(Vk vk, PhysicalDevice gpu, QueueFamilyIndiciesDetails indices, string[] deviceExtensions)
    {
        queueIndicesDetails = indices;
        Span<(uint queueFamily, uint count)> uniqueFamilyIndices = stackalloc (uint, uint)[4];
        uniqueFamilyIndices = uniqueFamilyIndices[..indices.GetUniqueFamilies(uniqueFamilyIndices)];
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
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions),
            EnabledLayerCount = 0
        };
        _ = vk.CreateDevice(gpu, &createInfo, null, out device);

        graphicsQueue = vk.GetDeviceQueue(device, indices.graphicsFamily, indices.graphicsQueueNum);
        presentQueue = vk.GetDeviceQueue(device, indices.presentFamily, indices.presentQueueNum);
        computeQueue = vk.GetDeviceQueue(device, indices.computeFamily, indices.computeQueueNum);
        transferQueue = vk.GetDeviceQueue(device, indices.transferFamily, indices.transferQueueNum);

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

        graphicsCmdPool = cmdPools[uniqueFamilyIndices.FindIndex(x=>x.queueFamily == indices.graphicsFamily)];
        presentCmdPool = cmdPools[uniqueFamilyIndices.FindIndex(x=>x.queueFamily == indices.presentFamily)];
        computeCmdPool = cmdPools[uniqueFamilyIndices.FindIndex(x=>x.queueFamily == indices.computeFamily)];
        transferCmdPool = cmdPools[uniqueFamilyIndices.FindIndex(x => x.queueFamily == indices.transferFamily)];

        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
    }
}
