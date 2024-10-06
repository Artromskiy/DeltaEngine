using Delta.Rendering.Internal;
using Delta.Utilities;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using System;

namespace Delta.Rendering.Windowed;

internal class RenderBase : Headless.RenderBase, IDisposable
{
    public readonly IWindow _window;


    private SurfaceKHR? _surface;
    private KhrSurface? _khrsf;
    private SwapChainSupportDetails? _swapChainSupport;
    private readonly ColorSpaceKHR _targetColorSpace = ColorSpaceKHR.SpaceAdobergbLinearExt;
    private readonly Format _targetFormat = Format.R8G8B8A8Unorm;
    private string[]? _instanceExtensions;
    private string[]? _deviceExtensions;
    public unsafe SurfaceKHR Surface => _surface ??=
        _window.VkSurface!.Create<nint>(new(instance.Handle), null).ToSurface();

    public KhrSurface Khrsf => _khrsf ??= GetKhrsf();
    public SwapChainSupportDetails SwapChainSupport => _swapChainSupport ??=
        new(gpu, Surface, Khrsf);
    public virtual SurfaceFormatKHR SurfaceFormat =>
        RenderHelper.ChooseSwapSurfaceFormat(SwapChainSupport.Formats, new(_targetFormat, _targetColorSpace));
    protected override unsafe ReadOnlySpan<string> InstanceExtensions => _instanceExtensions ??=
        [.. base.InstanceExtensions, .. RenderHelper.GetVulkanExtensions(_window)];
    protected override ReadOnlySpan<string> DeviceExtensions => _deviceExtensions ??=
        [.. base.DeviceExtensions, KhrSwapchain.ExtensionName];
    public override Format Format => SurfaceFormat.Format;
    protected override ImageLayout RenderPassFinalLayout => ImageLayout.PresentSrcKhr;

    public RenderBase(IWindow window, string appName) : base(appName)
    {
        _window = window;
    }

    protected override int DeviceSelector(PhysicalDevice device)
    {
        vk.GetPhysicalDeviceProperties(device, out var props);
        var suitable = RenderHelper.IsDeviceSuitable(vk, device, Surface, Khrsf, DeviceExtensions);
        var discrete = props.DeviceType == PhysicalDeviceType.DiscreteGpu ? 1 : 0;
        return suitable ? 1 + discrete : 0;
    }
    protected override DeviceQueues CreateLogicalDevice()
    {
        return WindowedRenderHelper.CreateLogicalDevice(vk, gpu, Surface, Khrsf, DeviceExtensions);
    }

    private KhrSurface GetKhrsf()
    {
        _ = vk.TryGetInstanceExtension<KhrSurface>(instance, out var khrsf);
        return khrsf;
    }

    public unsafe void Dispose()
    {
        base.Dispose();
        Khrsf.DestroySurface(instance, Surface, null);
    }

    public void UpdateSupportDetails()
    {
        _swapChainSupport = new SwapChainSupportDetails(gpu, Surface, Khrsf);
    }
}