using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace DeltaEngine
{
    internal struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete()
        {
            return GraphicsFamily.HasValue && PresentFamily.HasValue;
        }
    }

    internal struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }

    internal unsafe class HelloTriangleApplication : IDisposable
    {
        private const int WIDTH = 800;
        private const int HEIGHT = 600;
        private const int MAX_FRAMES_IN_FLIGHT = 2;
        private readonly bool EnableValidationLayers = false;

        private readonly string[] validationLayers = new[]
        {
        "VK_LAYER_KHRONOS_validation"
    };

        private readonly string[] deviceExtensions = new[]
        {
            KhrSwapchain.ExtensionName
        };

        private readonly Window* window;
        private Api _api;

        private Instance instance;

        private ExtDebugUtils? debugUtils;
        private DebugUtilsMessengerEXT debugMessenger;
        private KhrSurface? khrsf;
        private SurfaceKHR surface;

        private PhysicalDevice physicalDevice;
        private Device device;

        private Queue graphicsQueue;
        private Queue presentQueue;

        private KhrSwapchain? khrSwapChain;
        private SwapchainKHR swapChain;
        private Image[]? swapChainImages;
        private Format swapChainImageFormat;
        private Extent2D swapChainExtent;
        private ImageView[]? swapChainImageViews;
        private Framebuffer[]? swapChainFramebuffers;

        private RenderPass renderPass;
        private PipelineLayout pipelineLayout;
        private Pipeline? graphicsPipeline;

        private CommandPool commandPool;
        private CommandBuffer[]? commandBuffers;

        private Semaphore[]? imageAvailableSemaphores;
        private Semaphore[]? renderFinishedSemaphores;
        private Fence[]? inFlightFences;
        private Fence[]? imagesInFlight;
        private int currentFrame = 0;

        public HelloTriangleApplication()
        {
            _api = RenderHelper.CreateApi();
            window = RenderHelper.CreateWindow(_api, "Renderer");
            InitVulkan();
            MainLoop();
        }

        public void Dispose()
        {
            CleanUp();
        }

        private void InitVulkan()
        {
            instance = RenderHelper.CreateVkInstance(_api, window, "Lol", "kek");
            //SetupDebugMessenger();
            _ = _api.vk.TryGetInstanceExtension<KhrSurface>(instance, out khrsf);
            surface = RenderHelper.CreateSurface(_api, window, instance);
            physicalDevice = RenderHelper.PickPhysicalDevice(_api, instance, surface, khrsf, deviceExtensions);
            (device, graphicsQueue, presentQueue) = RenderHelper.CreateLogicalDevice(_api, physicalDevice, surface, khrsf, deviceExtensions);
            (khrSwapChain, swapChain, swapChainImages, swapChainImageFormat, swapChainExtent) = RenderHelper.CreateSwapChain(_api, window, instance, device, physicalDevice, surface, khrsf);
            swapChainImageViews = RenderHelper.CreateImageViews(_api, device, swapChainImages, swapChainImageFormat);
            renderPass = RenderHelper.CreateRenderPass(_api, device, swapChainImageFormat);
            //(graphicsPipeline, pipelineLayout) = RenderHelper.CreateGraphicsPipeline(_api, device, swapChainExtent, renderPass);
            swapChainFramebuffers = RenderHelper.CreateFramebuffers(_api, device, swapChainImageViews, renderPass, swapChainExtent);
            commandPool = RenderHelper.CreateCommandPool(_api, physicalDevice, device, surface, khrsf);
            commandBuffers = RenderHelper.CreateCommandBuffers(_api, swapChainFramebuffers, commandPool, device, renderPass, swapChainExtent, graphicsPipeline);
            //PickPhysicalDevice();
            //CreateLogicalDevice();
            //CreateSwapChain();
            //CreateImageViews();
            //CreateRenderPass();
            //CreateFramebuffers();
            //CreateCommandPool();
            //CreateCommandBuffers();
            CreateSyncObjects();
        }

        private void MainLoop()
        {
            _api.vk.DeviceWaitIdle(device);
        }

        private void CleanUp()
        {
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                _api.vk.DestroySemaphore(device, renderFinishedSemaphores![i], null);
                _api.vk.DestroySemaphore(device, imageAvailableSemaphores![i], null);
                _api.vk.DestroyFence(device, inFlightFences![i], null);
            }

            _api.vk.DestroyCommandPool(device, commandPool, null);

            foreach (var framebuffer in swapChainFramebuffers!)
            {
                _api.vk.DestroyFramebuffer(device, framebuffer, null);
            }

            if(graphicsPipeline.HasValue)
            _api.vk.DestroyPipeline(device, graphicsPipeline.Value, null);
            _api.vk.DestroyPipelineLayout(device, pipelineLayout, null);
            _api.vk.DestroyRenderPass(device, renderPass, null);

            foreach (var imageView in swapChainImageViews!)
            {
                _api.vk.DestroyImageView(device, imageView, null);
            }

            khrSwapChain!.DestroySwapchain(device, swapChain, null);

            _api.vk.DestroyDevice(device, null);

            if (EnableValidationLayers)
            {
                //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
                debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
            }

            khrsf!.DestroySurface(instance, surface, null);
            _api.vk.DestroyInstance(instance, null);
            _api.vk.Dispose();

            _api.sdl.DestroyWindow(window);
        }


        private void SetupDebugMessenger()
        {
            _ = !_api.vk.TryGetInstanceExtension(instance, out debugUtils);

            DebugUtilsMessengerCreateInfoEXT createInfo = new()
            {
                SType = StructureType.DebugUtilsMessengerCreateInfoExt,
                MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                         DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt,
                MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                     DebugUtilsMessageTypeFlagsEXT.ValidationBitExt,
                PfnUserCallback = (PfnDebugUtilsMessengerCallbackEXT)DebugCallback,
            };

            _ = debugUtils!.CreateDebugUtilsMessenger(instance, &createInfo, null, out debugMessenger);
        }

        private void CreateSwapChain()
        {
            var swapChainSupport = QuerySwapChainSupport(physicalDevice);

            var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
            var presentMode = ChoosePresentMode(swapChainSupport.PresentModes);
            var extent = ChooseSwapExtent(swapChainSupport.Capabilities);

            var imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            SwapchainCreateInfoKHR creatInfo = new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = surface,

                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            };

            var indices = FindQueueFamilies(physicalDevice);
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

            if (!_api.vk.TryGetDeviceExtension(instance, device, out khrSwapChain))
            {
                throw new NotSupportedException("VK_KHR_swapchain extension not found.");
            }

            if (khrSwapChain!.CreateSwapchain(device, creatInfo, null, out swapChain) != Result.Success)
            {
                throw new Exception("failed to create swap chain!");
            }

            khrSwapChain.GetSwapchainImages(device, swapChain, ref imageCount, null);
            swapChainImages = new Image[imageCount];
            fixed (Image* swapChainImagesPtr = swapChainImages)
            {
                khrSwapChain.GetSwapchainImages(device, swapChain, ref imageCount, swapChainImagesPtr);
            }

            swapChainImageFormat = surfaceFormat.Format;
            swapChainExtent = extent;
        }

        private void CreateImageViews()
        {
            swapChainImageViews = new ImageView[swapChainImages!.Length];

            for (int i = 0; i < swapChainImages.Length; i++)
            {
                ImageViewCreateInfo createInfo = new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = swapChainImages[i],
                    ViewType = ImageViewType.Type2D,
                    Format = swapChainImageFormat,
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

                if (_api.vk.CreateImageView(device, createInfo, null, out swapChainImageViews[i]) != Result.Success)
                {
                    throw new Exception("failed to create image views!");
                }
            }
        }

        private void CreateRenderPass()
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

            if (_api.vk.CreateRenderPass(device, renderPassInfo, null, out renderPass) != Result.Success)
            {
                throw new Exception("failed to create render pass!");
            }
        }


        private void CreateFramebuffers()
        {
            swapChainFramebuffers = new Framebuffer[swapChainImageViews!.Length];

            for (int i = 0; i < swapChainImageViews.Length; i++)
            {
                var attachment = swapChainImageViews[i];

                FramebufferCreateInfo framebufferInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = renderPass,
                    AttachmentCount = 1,
                    PAttachments = &attachment,
                    Width = swapChainExtent.Width,
                    Height = swapChainExtent.Height,
                    Layers = 1,
                };

                if (_api.vk.CreateFramebuffer(device, framebufferInfo, null, out swapChainFramebuffers[i]) != Result.Success)
                {
                    throw new Exception("failed to create framebuffer!");
                }
            }
        }

        private void CreateCommandPool()
        {
            var queueFamiliyIndicies = FindQueueFamilies(physicalDevice);

            CommandPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamiliyIndicies.GraphicsFamily!.Value,
            };

            if (_api.vk.CreateCommandPool(device, poolInfo, null, out commandPool) != Result.Success)
            {
                throw new Exception("failed to create command pool!");
            }
        }

        private void CreateSyncObjects()
        {
            imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
            renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
            inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
            imagesInFlight = new Fence[swapChainImages!.Length];

            SemaphoreCreateInfo semaphoreInfo = new()
            {
                SType = StructureType.SemaphoreCreateInfo,
            };

            FenceCreateInfo fenceInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.SignaledBit,
            };

            for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                _ = _api.vk.CreateSemaphore(device, semaphoreInfo, null, out imageAvailableSemaphores[i]);
                _ = _api.vk.CreateSemaphore(device, semaphoreInfo, null, out renderFinishedSemaphores[i]);
                _ = _api.vk.CreateFence(device, fenceInfo, null, out inFlightFences[i]);
            }
        }

        public void Update()
        {
            _api.sdl.PollEvent((Silk.NET.SDL.Event*)null);
        }

        public void DrawFrame()
        {
            _api.vk.WaitForFences(device, 1, inFlightFences![currentFrame], true, ulong.MaxValue);

            uint imageIndex = 0;
            khrSwapChain!.AcquireNextImage(device, swapChain, ulong.MaxValue, imageAvailableSemaphores![currentFrame], default, ref imageIndex);

            if (imagesInFlight![imageIndex].Handle != default)
            {
                _api.vk.WaitForFences(device, 1, imagesInFlight[imageIndex], true, ulong.MaxValue);
            }
            imagesInFlight[imageIndex] = inFlightFences[currentFrame];

            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
            };

            var waitSemaphores = stackalloc[] { imageAvailableSemaphores[currentFrame] };
            var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

            var buffer = commandBuffers![imageIndex];

            submitInfo = submitInfo with
            {
                WaitSemaphoreCount = 1,
                PWaitSemaphores = waitSemaphores,
                PWaitDstStageMask = waitStages,

                CommandBufferCount = 1,
                PCommandBuffers = &buffer
            };

            var signalSemaphores = stackalloc[] { renderFinishedSemaphores![currentFrame] };
            submitInfo = submitInfo with
            {
                SignalSemaphoreCount = 1,
                PSignalSemaphores = signalSemaphores,
            };

            _api.vk.ResetFences(device, 1, inFlightFences[currentFrame]);

            _ = _api.vk.QueueSubmit(graphicsQueue, 1, submitInfo, inFlightFences[currentFrame]);
            var swapChains = stackalloc[] { swapChain };
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,

                WaitSemaphoreCount = 1,
                PWaitSemaphores = signalSemaphores,

                SwapchainCount = 1,
                PSwapchains = swapChains,

                PImageIndices = &imageIndex
            };

            khrSwapChain.QueuePresent(presentQueue, presentInfo);

            currentFrame = (currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;

        }

        private ShaderModule CreateShaderModule(byte[] code)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
            };

            ShaderModule shaderModule;

            fixed (byte* codePtr = code)
            {
                createInfo.PCode = (uint*)codePtr;

                if (_api.vk.CreateShaderModule(device, createInfo, null, out shaderModule) != Result.Success)
                {
                    throw new Exception();
                }
            }

            return shaderModule;

        }

        private SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
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

        private PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
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

        private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }
            else
            {
                int w, h;
                w = h = 0;
                _api.sdl.VulkanGetDrawableSize(window, ref w, ref h);
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

        private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice)
        {
            var details = new SwapChainSupportDetails();

            khrsf!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out details.Capabilities);

            uint formatCount = 0;
            khrsf.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, null);

            if (formatCount != 0)
            {
                details.Formats = new SurfaceFormatKHR[formatCount];
                fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
                {
                    khrsf.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, formatsPtr);
                }
            }
            else
            {
                details.Formats = Array.Empty<SurfaceFormatKHR>();
            }

            uint presentModeCount = 0;
            khrsf.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, null);

            if (presentModeCount != 0)
            {
                details.PresentModes = new PresentModeKHR[presentModeCount];
                fixed (PresentModeKHR* formatsPtr = details.PresentModes)
                {
                    khrsf.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, formatsPtr);
                }

            }
            else
            {
                details.PresentModes = Array.Empty<PresentModeKHR>();
            }

            return details;
        }

        private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            var indices = new QueueFamilyIndices();

            uint queueFamilityCount = 0;
            _api.vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

            var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
            fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
            {
                _api.vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
            }


            uint i = 0;
            foreach (var queueFamily in queueFamilies)
            {
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                {
                    indices.GraphicsFamily = i;
                }

                khrsf!.GetPhysicalDeviceSurfaceSupport(device, i, surface, out var presentSupport);

                if (presentSupport)
                {
                    indices.PresentFamily = i;
                }

                if (indices.IsComplete())
                {
                    break;
                }

                i++;
            }

            return indices;
        }

        private uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
        {
            Console.WriteLine($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));
            return Vk.False;
        }
    }
}