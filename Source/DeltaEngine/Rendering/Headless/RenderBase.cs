using Delta.Rendering.Internal;
using Delta.Utilities;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Delta.Rendering.Headless;

internal class RenderBase : IDisposable
{
    public readonly string appName;
    public readonly Vk vk = Vk.GetApi();
    public readonly Instance instance;
    public readonly Gpu gpu;

    public readonly DeviceQueues deviceQ;

    public readonly DescriptorPool descriptorPool;

    public readonly PipelineLayout pipelineLayout;

    public readonly RenderPass renderPass;

    public readonly CommonDescriptorSetLayouts descriptorSetLayouts;

    private const string RendererName = "Delta Renderer";

    private static readonly string[] _validationLayers = ["VK_LAYER_KHRONOS_validation"];
    protected virtual ReadOnlySpan<string> Layers => ValidationLayerSupported() ?
        _validationLayers : ReadOnlySpan<string>.Empty;

    private static readonly string[] _instanceExtensions = [ExtDebugUtils.ExtensionName];
    protected virtual ReadOnlySpan<string> InstanceExtensions => DebugExtensionSupported() ?
        _instanceExtensions : ReadOnlySpan<string>.Empty;

    protected virtual ReadOnlySpan<string> DeviceExtensions => [];
    public virtual Format Format => Format.R8G8B8A8Unorm;
    protected virtual ImageLayout RenderPassFinalLayout => ImageLayout.TransferSrcOptimal;

    public unsafe RenderBase(string appName)
    {
        this.appName = appName;
        bool validationSupported = ValidationLayerSupported();
        bool debugUtilsSupported = vk.IsInstanceExtensionPresent(ExtDebugUtils.ExtensionName);
        DebugUtilsMessengerCreateInfoEXT debugCreateInfo = debugUtilsSupported ?
            PopulateDebugMessengerCreateInfo() : default;

        instance = RenderHelper.CreateVkInstance(vk, appName, RendererName, InstanceExtensions, Layers, debugUtilsSupported ? &debugCreateInfo : null);

        gpu = RenderHelper.PickPhysicalDevice(vk, instance, DeviceSelector);

        deviceQ = CreateLogicalDevice();

        descriptorPool = RenderHelper.CreateDescriptorPool(vk, deviceQ);

        descriptorSetLayouts = new CommonDescriptorSetLayouts(vk, deviceQ);

        pipelineLayout = RenderHelper.CreatePipelineLayout(vk, deviceQ, descriptorSetLayouts.Layouts);

        renderPass = RenderHelper.CreateRenderPass(vk, deviceQ, Format, RenderPassFinalLayout);
    }
    protected virtual int DeviceSelector(PhysicalDevice device)
    {
        vk.GetPhysicalDeviceProperties(device, out var props);
        var suitable = RenderHelper.IsDeviceSuitable(vk, device, DeviceExtensions);
        var discrete = props.DeviceType == PhysicalDeviceType.DiscreteGpu ? 1 : 0;
        return suitable ? 1 + discrete : 0;
    }
    protected virtual DeviceQueues CreateLogicalDevice()
    {
        Span<QueueType> neededQueues = [QueueType.Graphics, QueueType.Compute, QueueType.Transfer];
        return RenderHelper.CreateLogicalDevice(vk, gpu, neededQueues, DeviceExtensions);
    }

    public unsafe void Dispose()
    {
        vk.DestroyRenderPass(deviceQ, renderPass, null);
        vk.DestroyPipelineLayout(deviceQ, pipelineLayout, null);
        descriptorSetLayouts.Dispose();
        vk.DestroyDescriptorPool(deviceQ, descriptorPool, null);
        deviceQ.Dispose();
        vk.DestroyInstance(instance, null);
    }

    private unsafe bool ValidationLayerSupported()
    {
        uint layerCount = 0;
        vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        Span<LayerProperties> availableLayers = stackalloc LayerProperties[(int)layerCount];
        vk.EnumerateInstanceLayerProperties(&layerCount, availableLayers);
        foreach (var item in _validationLayers)
            if (!availableLayers.Exist(layer => Marshal.PtrToStringAnsi((nint)layer.LayerName) == item))
                return false;
        return true;
    }

    private bool DebugExtensionSupported() => vk.IsInstanceExtensionPresent(ExtDebugUtils.ExtensionName);

    private unsafe DebugUtilsMessengerCreateInfoEXT PopulateDebugMessengerCreateInfo()
    {
        var severityFlags = Enums.GetValues<DebugUtilsMessageSeverityFlagsEXT>();
        var messageTypeFlags = Enums.GetValues<DebugUtilsMessageTypeFlagsEXT>();
        var allSeverity = severityFlags[0];
        var allmessageTypes = messageTypeFlags[0];
        for (int i = 1; i < severityFlags.Length; i++)
            allSeverity |= severityFlags[i];
        for (int i = 1; i < messageTypeFlags.Length; i++)
            allmessageTypes |= messageTypeFlags[i];

        DebugUtilsMessengerCreateInfoEXT createInfo = new()
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = allSeverity,
            MessageType = allmessageTypes,
            PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(DebugCallback)
        };
        return createInfo;
    }

    private unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        try
        {
            var type = messageTypes.ToString();
            var message = (nint)pCallbackData->PMessage;
            var messageString = Marshal.PtrToStringAnsi(message);
            bool assertFail =
                (messageTypes.HasFlag(DebugUtilsMessageTypeFlagsEXT.ValidationBitExt) ||
                messageTypes.HasFlag(DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt)) &&
                (messageSeverity.HasFlag(DebugUtilsMessageSeverityFlagsEXT.WarningBitExt) ||
                messageSeverity.HasFlag(DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt));

            var messageTypeString = Enums.ToString(messageTypes);
            var messageSeverityString = Enums.ToString(messageSeverity);
            Console.WriteLine($"Severity {messageSeverity} {messageTypeString}:");
            Console.WriteLine(messageString);
            Debug.Assert(!assertFail, messageString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return Vk.True;
    }
}