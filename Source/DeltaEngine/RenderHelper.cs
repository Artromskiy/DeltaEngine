using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DeltaEngine
{

    public static class RenderHelper
    {
        public static Api CreateApi()
        {
            Api res = new()
            {
                sdl = Sdl.GetApi(),
                vk = Vk.GetApi(),
            };
            res.sdl.Init(Sdl.InitVideo | Sdl.InitEvents);
            return res;
        }

        private const uint flags = (uint)
        (
              WindowFlags.Vulkan 
            | WindowFlags.Shown
            | WindowFlags.Resizable
//            | WindowFlags.AllowHighdpi 
            | WindowFlags.Borderless 
//            | WindowFlags.FullscreenDesktop
       );

        public static unsafe Window* CreateWindow(Api api, string title)
        {
            return api.sdl.CreateWindow(Encoding.UTF8.GetBytes(title), 100, 100, 500, 500, flags);
        }

        public static unsafe Instance CreateVkInstance(Api api, Window* window, string app, string engine)
        {
            ApplicationInfo appInfo = new()
            {
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi(engine),
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(app),
                EngineVersion = new Version32(1, 0, 0),
                ApplicationVersion = new Version32(1, 0, 0),
                ApiVersion = Vk.Version10
            };
            var extensions = GetVulkanExtensions(api, window);
            InstanceCreateInfo createInfo = new()
            {
                PApplicationInfo = &appInfo,
                EnabledExtensionCount = (uint)extensions.Length,
                PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions),
                EnabledLayerCount = 0,
                PNext = null,
            };
            _ = api.vk.CreateInstance(createInfo, null, out var instance);
            Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
            Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
            return instance;
        }

        public static unsafe RenderPass CreateRenderPass(Api api, Device device, Format swapChainImageFormat)
        {
            AttachmentDescription colorAttachment = new()
            {
                Format = swapChainImageFormat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
            };

            AttachmentReference colorAttachmentRef = new()
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal,
            };

            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
            };

            SubpassDependency dependency = new()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit
            };

            RenderPassCreateInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 1,
                PAttachments = &colorAttachment,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency,
            };

            _ = api.vk.CreateRenderPass(device, renderPassInfo, null, out var renderPass);
            return renderPass;
        }

        public static unsafe string[] GetVulkanExtensions(Api api, Window* window)
        {
            uint extCount = 0;
            _ = api.sdl.VulkanGetInstanceExtensions(window, ref extCount, (byte**)null);
            string[] extensions = new string[extCount];
            _ = api.sdl.VulkanGetInstanceExtensions(window, ref extCount, extensions);
            var res = new List<string>();
            res.AddRange(extensions);
            res.Add(ExtDebugReport.ExtensionName);
            return res.ToArray();
        }

        public static unsafe string[] GetVulkanLayers(Api api, string[] reqVkLayers)
        {
            uint layersCount = 0;
            _ = api.vk.EnumerateInstanceLayerProperties(&layersCount, null);
            Span<LayerProperties> layers = stackalloc LayerProperties[(int)layersCount];
            _ = api.vk.EnumerateInstanceLayerProperties(&layersCount, layers);
            HashSet<string> layersNames = new();
            foreach (var layer in layers)
            {
                var layerName = Marshal.PtrToStringUTF8((IntPtr)layer.LayerName);
                if (!string.IsNullOrEmpty(layerName))
                    layersNames.Add(layerName);
            }
            return Array.FindAll(reqVkLayers, layersNames.Contains);
        }


        public static unsafe SurfaceKHR CreateSurface(Api api, Window* window, Instance instance)
        {
            var nondispatchable = new VkNonDispatchableHandle();
            _ = api.sdl.VulkanCreateSurface(window, instance.ToHandle(), ref nondispatchable);
            return nondispatchable.ToSurface();
        }


        public static unsafe PhysicalDevice PickPhysicalDevice(Api api, Instance instance, SurfaceKHR surface, KhrSurface khrsf, string[] neededExtensions)
        {
            uint devicedCount = 0;
            _ = api.vk.EnumeratePhysicalDevices(instance, &devicedCount, null);
            _ = devicedCount != 0;
            Span<PhysicalDevice> devices = stackalloc PhysicalDevice[(int)devicedCount];
            api.vk.EnumeratePhysicalDevices(instance, &devicedCount, devices);
            PhysicalDevice physicalDevice = default;
            foreach (var device in devices)
            {
                if (IsDeviceSuitable(api, device, surface, khrsf, neededExtensions))
                {
                    physicalDevice = device;
                    break;
                }
            }
            _ = physicalDevice.Handle != 0;
            return physicalDevice;
        }

        public static unsafe ImageView[] CreateImageViews(Api api, Device device, Image[] swapChainImages, Format swapchainImageFormat)
        {
            var swapChainImageViews = new ImageView[swapChainImages.Length];

            for (int i = 0; i < swapChainImages.Length; i++)
            {
                ImageViewCreateInfo createInfo = new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = swapChainImages[i],
                    ViewType = ImageViewType.Type2D,
                    Format = swapchainImageFormat,
                    Components = 
                    {
                        R = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        B = ComponentSwizzle.Identity,
                        A = ComponentSwizzle.Identity,
                    },
                    SubresourceRange =
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                };
                _ = api.vk.CreateImageView(device, createInfo, null, out swapChainImageViews[i]);
            }
            return swapChainImageViews;
        }


        public static unsafe (Device device, Queue graphicsQueue, Queue presentQueue) CreateLogicalDevice(Api api, PhysicalDevice gpu, SurfaceKHR surface, KhrSurface khrsf, string[] deviceExtensions)
        {
            var indices = FindQueueFamilies(api, gpu, surface, khrsf);

            var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };
            uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

            using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
            var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

            float queuePriority = 1.0f;
            for (int i = 0; i < uniqueQueueFamilies.Length; i++)
            {
                queueCreateInfos[i] = new()
                {
                    QueueFamilyIndex = uniqueQueueFamilies[i],
                    QueueCount = 1,
                    PQueuePriorities = &queuePriority
                };
            }

            PhysicalDeviceFeatures deviceFeatures = new();

            DeviceCreateInfo createInfo = new()
            {
                QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
                PQueueCreateInfos = queueCreateInfos,

                PEnabledFeatures = &deviceFeatures,

                EnabledExtensionCount = (uint)deviceExtensions.Length,
                PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions),
                EnabledLayerCount = 0
            };

            _ = api.vk.CreateDevice(gpu, &createInfo, null, out var device);

            api.vk.GetDeviceQueue(device, indices.GraphicsFamily!.Value, 0, out var graphicsQueue);
            api.vk.GetDeviceQueue(device, indices.PresentFamily!.Value, 0, out var presentQueue);

            SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
            return (device, graphicsQueue, presentQueue);
        }


        public static unsafe (KhrSwapchain khrsw, SwapchainKHR sw, Image[] swImages, Format swImFormat, Extent2D extent)
            CreateSwapChain(Api api, Window* window, Instance instance, Device device, PhysicalDevice physicalDevice, SurfaceKHR surface, KhrSurface khrsf)
        {
            var swapChainSupport = QuerySwapChainSupport(physicalDevice, surface, khrsf);

            var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
            var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
            var extent = ChooseSwapExtent(api, window, swapChainSupport.Capabilities);

            var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            SwapchainCreateInfoKHR creatInfo = new()
            {
                Surface = surface,

                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            };

            var indices = FindQueueFamilies(api, physicalDevice, surface, khrsf);
            var queueFamilyIndices = stackalloc[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

            if (indices.GraphicsFamily != indices.PresentFamily)
            {
                creatInfo = creatInfo with
                {
                    ImageSharingMode = SharingMode.Concurrent,
                    QueueFamilyIndexCount = 2,
                    PQueueFamilyIndices = queueFamilyIndices,
                };
            }
            else
            {
                creatInfo.ImageSharingMode = SharingMode.Exclusive;
            }

            creatInfo = creatInfo with
            {
                PreTransform = swapChainSupport.Capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,

                OldSwapchain = default
            };

            _ = api.vk.TryGetDeviceExtension<KhrSwapchain>(instance, device, out var khrsw);
            _ = khrsw.CreateSwapchain(device, creatInfo, null, out var sw);

            khrsw.GetSwapchainImages(device, sw, ref imageCount, null);
            var swImages = new Image[imageCount];
            fixed (Image* swapChainImagesPtr = swImages)
                khrsw.GetSwapchainImages(device, sw, ref imageCount, swapChainImagesPtr);

            return (khrsw, sw, swImages, surfaceFormat.Format, extent);
        }


        private static SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
        {
            foreach (var availableFormat in availableFormats)
            {
                if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
                {
                    return availableFormat;
                }
            }

            return availableFormats[0];
        }

        private static PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
        {
            foreach (var availablePresentMode in availablePresentModes)
            {
                if (availablePresentMode == PresentModeKHR.MailboxKhr)
                {
                    return availablePresentMode;
                }
            }

            return PresentModeKHR.FifoKhr;
        }

        private static unsafe Extent2D ChooseSwapExtent(Api api, Window* window, SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                int w, h;
                w = h = 0;
                api.sdl.VulkanGetDrawableSize(window, ref w, ref h);
                var framebufferSize = new Extent2D((uint)w, (uint)h);
                //var framebufferSize = window!.FramebufferSize;

                Extent2D actualExtent = new()
                {
                    Width = framebufferSize.Width,
                    Height = framebufferSize.Height
                };

                actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
                actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

                return actualExtent;
            }
        }


        private static unsafe bool IsDeviceSuitable(Api api, PhysicalDevice device, SurfaceKHR surface, KhrSurface khrsf, string[] neededExtensions)
        {
            var indices = FindQueueFamilies(api, device, surface, khrsf);

            bool extensionsSupported = CheckDeviceExtensionsSupport(api, device, neededExtensions);

            bool swapChainAdequate = false;
            if (extensionsSupported)
            {
                var swapChainSupport = QuerySwapChainSupport(device, surface, khrsf);
                swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
            }

            return indices.IsComplete() && extensionsSupported && swapChainAdequate;
        }


        private static unsafe QueueFamilyIndices FindQueueFamilies(Api api, PhysicalDevice device, SurfaceKHR surface, KhrSurface khrsf)
        {
            var indices = new QueueFamilyIndices();
            uint queueFamilityCount = 0;
            api.vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilityCount, null);
            Span<QueueFamilyProperties> queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilityCount];
            api.vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilityCount, queueFamilies);
            for (uint i = 0; i < queueFamilies.Length; i++)
            {
                var queueFamily = queueFamilies[(int)i];
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                    indices.GraphicsFamily = i;

                khrsf!.GetPhysicalDeviceSurfaceSupport(device, i, surface, out var presentSupport);

                if (presentSupport)
                    indices.PresentFamily = i;

                if (indices.IsComplete())
                    break;
                i++;
            }
            return indices;
        }

        private static unsafe SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice, SurfaceKHR surface, KhrSurface khrsf)
        {
            var details = new SwapChainSupportDetails();
            _ = khrsf.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out details.Capabilities);

            uint formatCount = 0;
            _ = khrsf.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, null);

            if (formatCount != 0)
            {
                details.Formats = new SurfaceFormatKHR[formatCount];
                fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
                    _ = khrsf.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, &formatCount, formatsPtr);
            }
            else
            {
                details.Formats = Array.Empty<SurfaceFormatKHR>();
            }

            uint presentModeCount = 0;
            _ = khrsf.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &presentModeCount, null);

            if (presentModeCount != 0)
            {
                details.PresentModes = new PresentModeKHR[presentModeCount];
                fixed (PresentModeKHR* formatsPtr = details.PresentModes)
                    _ = khrsf.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, &presentModeCount, formatsPtr);

            }
            else
            {
                details.PresentModes = Array.Empty<PresentModeKHR>();
            }

            return details;
        }


        private static unsafe bool CheckDeviceExtensionsSupport(Api api, PhysicalDevice device, string[] neededExtensions)
        {
            uint extentionsCount = 0;
            api.vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extentionsCount, null);
            Span<ExtensionProperties> availableExtensions = stackalloc ExtensionProperties[(int)extentionsCount];
            api.vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extentionsCount, availableExtensions);

            foreach (var needed in neededExtensions)
                if (!availableExtensions.Exist(present => needed == Marshal.PtrToStringAnsi((IntPtr)present.ExtensionName)))
                    return false;
            return true;
        }

    }


}
