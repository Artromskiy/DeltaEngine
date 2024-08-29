using Delta.Rendering.Internal;
using Delta.Utilities;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;

namespace Delta.Rendering.SdlRendering;

internal sealed class RenderBase : HeadlessRendering.RenderBase, IDisposable
{
    public readonly Sdl sdl = Sdl.GetApi();

    private unsafe Window* _window;
    public unsafe Window* Window => _window == default ?
        _window = RenderHelper.CreateWindow(sdl, appName) : _window;

    private SurfaceKHR? _surface;
    public unsafe SurfaceKHR Surface => _surface ??=
        RenderHelper.CreateSurface(sdl, Window, instance);

    private KhrSurface? _khrsf;
    public KhrSurface Khrsf => _khrsf ??=
        GetKhrsf();

    private SwapChainSupportDetails? _swapChainSupport;
    public SwapChainSupportDetails SwapChainSupport => _swapChainSupport ??=
        new SwapChainSupportDetails(gpu, Surface, Khrsf);

    public override SurfaceFormatKHR Format => RenderHelper.ChooseSwapSurfaceFormat(SwapChainSupport.Formats, base.Format);

    private string[]? _instanceExtensions;
    protected override unsafe ReadOnlySpan<string> InstanceExtensions => _instanceExtensions ??=
        [.. base.InstanceExtensions, .. RenderHelper.GetSdlVulkanExtensions(sdl, Window)];

    private string[]? _deviceExtensions;
    protected override ReadOnlySpan<string> DeviceExtensions => _deviceExtensions ??=
        [.. base.DeviceExtensions, KhrSwapchain.ExtensionName];

    public RenderBase(string appName) : base(appName) { }

    protected override int DeviceSelector(PhysicalDevice device)
    {
        vk.GetPhysicalDeviceProperties(device, out var props);
        var suitable = RenderHelper.IsDeviceSuitable(vk, device, Surface, Khrsf, DeviceExtensions);
        var discrete = props.DeviceType == PhysicalDeviceType.DiscreteGpu ? 1 : 0;
        return suitable ? 1 + discrete : 0;
    }
    protected override DeviceQueues CreateLogicalDevice() => RenderHelper.CreateLogicalDevice(vk, gpu, Surface, Khrsf, DeviceExtensions);


    private KhrSurface GetKhrsf()
    {
        _ = vk.TryGetInstanceExtension<KhrSurface>(instance, out var khrsf);
        return khrsf;
    }

    public unsafe void Dispose()
    {
        Khrsf.DestroySurface(instance, Surface, null);
        sdl.DestroyWindow(Window);
    }

    public void UpdateSupportDetails()
    {
        _swapChainSupport = new SwapChainSupportDetails(gpu, Surface, Khrsf);
    }
}