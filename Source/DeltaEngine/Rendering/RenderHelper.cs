using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static DeltaEngine.ThrowHelper;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace DeltaEngine.Rendering;

public static class RenderHelper
{

    private const uint flags = (uint)
    (
          WindowFlags.Vulkan
        | WindowFlags.Shown
        | WindowFlags.Resizable
        | WindowFlags.AllowHighdpi
        | WindowFlags.Borderless
   //            | WindowFlags.FullscreenDesktop
   );

    public static unsafe Window* CreateWindow(Sdl sdl, string title)
    {
        return sdl.CreateWindow(Encoding.UTF8.GetBytes(title), 100, 100, 1000, 1000, flags);
    }

    public static unsafe Instance CreateVkInstance(Vk vk, Sdl sdl, Window* window, string app, string engine)
    {
        ApplicationInfo appInfo = new()
        {
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi(engine),
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(app),
            EngineVersion = new Version32(1, 0, 0),
            ApplicationVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version10
        };
        var extensions = GetVulkanExtensions(sdl, window);
        InstanceCreateInfo createInfo = new()
        {
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = (uint)extensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions),
            EnabledLayerCount = 0,
            PNext = null,
        };
        _ = vk.CreateInstance(createInfo, null, out var instance);
        Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
        return instance;
    }

    public static unsafe (Semaphore[] imageAvailableSemaphores, Semaphore[] renderFinishedSemaphores, Fence[] inFlightFences)
        CreateSyncObjects(Api api, Device device, int swapChainImagesCount)
    {
        var imageAvailableSemaphores = new Semaphore[swapChainImagesCount];
        var renderFinishedSemaphores = new Semaphore[swapChainImagesCount];
        var inFlightFences = new Fence[swapChainImagesCount];

        SemaphoreCreateInfo semaphoreInfo = new();

        FenceCreateInfo fenceInfo = new()
        {
            Flags = FenceCreateFlags.SignaledBit,
        };

        for (var i = 0; i < swapChainImagesCount; i++)
        {
            _ = api.vk.CreateSemaphore(device, semaphoreInfo, null, out imageAvailableSemaphores[i]);
            _ = api.vk.CreateSemaphore(device, semaphoreInfo, null, out renderFinishedSemaphores[i]);
            _ = api.vk.CreateFence(device, fenceInfo, null, out inFlightFences[i]);
        }
        return (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences);
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

    public static unsafe string[] GetVulkanExtensions(Sdl sdl, Window* window)
    {
        uint extCount = 0;
        _ = sdl.VulkanGetInstanceExtensions(window, ref extCount, (byte**)null);
        string[] extensions = new string[extCount];
        _ = sdl.VulkanGetInstanceExtensions(window, ref extCount, extensions);
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
            var layerName = Marshal.PtrToStringUTF8((nint)layer.LayerName);
            if (!string.IsNullOrEmpty(layerName))
                layersNames.Add(layerName);
        }
        return Array.FindAll(reqVkLayers, layersNames.Contains);
    }


    public static unsafe SurfaceKHR CreateSurface(Sdl sdl, Window* window, Instance instance)
    {
        var nondispatchable = new VkNonDispatchableHandle();
        _ = sdl.VulkanCreateSurface(window, instance.ToHandle(), ref nondispatchable);
        return nondispatchable.ToSurface();
    }


    public static unsafe (Pipeline pipeline, PipelineLayout layout) CreateGraphicsPipeline(Api api, Device device, Extent2D swapChainExtent, RenderPass renderPass)
    {
        var vertShaderCode = File.ReadAllBytes("shaders/vert.spv");
        var fragShaderCode = File.ReadAllBytes("shaders/frag.spv");

        var vertShaderModule = CreateShaderModule(api.vk, device, vertShaderCode);
        var fragShaderModule = CreateShaderModule(api.vk, device, fragShaderCode);


        PipelineShaderStageCreateInfo vertShaderStageInfo = new()
        {
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        PipelineShaderStageCreateInfo fragShaderStageInfo = new()
        {
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)SilkMarshal.StringToPtr("main")
        };

        var shaderStages = stackalloc PipelineShaderStageCreateInfo[]
        {
            vertShaderStageInfo,
            fragShaderStageInfo
        };

        PipelineVertexInputStateCreateInfo vertexInputInfo = new()
        {
            VertexBindingDescriptionCount = 0,
            VertexAttributeDescriptionCount = 0,
        };

        PipelineInputAssemblyStateCreateInfo inputAssembly = new()
        {
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };

        Viewport viewport = new()
        {
            Width = swapChainExtent.Width,
            Height = swapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };

        Rect2D scissor = new(extent: swapChainExtent);

        PipelineViewportStateCreateInfo viewportState = new()
        {
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor,
        };

        PipelineRasterizationStateCreateInfo rasterizer = new()
        {
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = false,
        };

        PipelineMultisampleStateCreateInfo multisampling = new()
        {
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };

        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false,
        };

        PipelineColorBlendStateCreateInfo colorBlending = new()
        {
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment,
        };

        PipelineLayoutCreateInfo pipelineLayoutInfo = new();
        _ = api.vk.CreatePipelineLayout(device, pipelineLayoutInfo, null, out var pipelineLayout);
        GraphicsPipelineCreateInfo pipelineInfo = new()
        {
            StageCount = 2,
            PStages = shaderStages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PColorBlendState = &colorBlending,
            Layout = pipelineLayout,
            RenderPass = renderPass,
            Subpass = 0,
            BasePipelineHandle = default,
        };
        _ = api.vk.CreateGraphicsPipelines(device, default, 1, pipelineInfo, null, out var graphicsPipeline);
        api.vk.DestroyShaderModule(device, fragShaderModule, null);
        api.vk.DestroyShaderModule(device, vertShaderModule, null);

        SilkMarshal.Free((nint)vertShaderStageInfo.PName);
        SilkMarshal.Free((nint)fragShaderStageInfo.PName);
        return (graphicsPipeline, pipelineLayout);
    }


    public static unsafe (Pipeline pipeline, PipelineLayout layout) CreateGraphicsPipeline(RenderBase data, Extent2D swapChainExtent, RenderPass renderPass)
    {
        using var vertShader = new Shader(data, ShaderStageFlags.VertexBit, "shaders/vert.spv");
        using var fragShader = new Shader(data, ShaderStageFlags.FragmentBit, "shaders/frag.spv");
        using var groupCreator = new ShaderModuleGroupCreator();

        var stages = stackalloc PipelineShaderStageCreateInfo[2]
        {
            groupCreator.Create(vertShader),
            groupCreator.Create(fragShader)
        };

        PipelineVertexInputStateCreateInfo vertexInputInfo = new()
        {
            VertexBindingDescriptionCount = 0,
            VertexAttributeDescriptionCount = 0,
        };

        PipelineInputAssemblyStateCreateInfo inputAssembly = new()
        {
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };

        Viewport viewport = new()
        {
            Width = swapChainExtent.Width,
            Height = swapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };

        Rect2D scissor = new(extent: swapChainExtent);

        PipelineViewportStateCreateInfo viewportState = new()
        {
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor,
        };

        PipelineRasterizationStateCreateInfo rasterizer = new()
        {
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = false,
        };

        PipelineMultisampleStateCreateInfo multisampling = new()
        {
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };

        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false,
        };

        PipelineColorBlendStateCreateInfo colorBlending = new()
        {
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment,
        };

        PipelineLayoutCreateInfo pipelineLayoutInfo = new();
        _ = data.vk.CreatePipelineLayout(data.device, pipelineLayoutInfo, null, out var pipelineLayout);

        GraphicsPipelineCreateInfo pipelineInfo = new()
        {
            StageCount = 2,
            PStages = stages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PColorBlendState = &colorBlending,
            Layout = pipelineLayout,
            RenderPass = renderPass,
            Subpass = 0,
            BasePipelineHandle = default,
        };
        _ = data.vk.CreateGraphicsPipelines(data.device, default, 1, pipelineInfo, null, out var graphicsPipeline);

        return (graphicsPipeline, pipelineLayout);
    }


    private static unsafe ShaderModule CreateShaderModule(Vk vk, Device device, byte[] shaderCode)
    {
        ShaderModuleCreateInfo createInfo = new()
        {
            CodeSize = (nuint)shaderCode.Length,
        };
        using pin<byte> hn = new(shaderCode);
        createInfo.PCode = (uint*)hn.handle;
        _ = vk.CreateShaderModule(device, createInfo, null, out ShaderModule shaderModule);
        return shaderModule;
    }


    public static unsafe ImmutableArray<Framebuffer> CreateFramebuffers(Api api, Device device, ReadOnlySpan<ImageView> swapChainImageViews, RenderPass renderPass, Extent2D swapChainExtent)
    {
        Span<Framebuffer> swapChainFramebuffers = stackalloc Framebuffer[swapChainImageViews.Length];
        for (int i = 0; i < swapChainImageViews.Length; i++)
        {
            var attachment = swapChainImageViews[i];
            FramebufferCreateInfo framebufferInfo = new()
            {
                RenderPass = renderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = swapChainExtent.Width,
                Height = swapChainExtent.Height,
                Layers = 1,
            };
            _ = api.vk.CreateFramebuffer(device, framebufferInfo, null, out swapChainFramebuffers[i]);
        }
        return ImmutableArray.Create(swapChainFramebuffers);
    }

    public static unsafe CommandPool CreateCommandPool(Api api, Instance instance, PhysicalDevice gpu, Device device, SurfaceKHR surface)
    {
        api.vk.TryGetInstanceExtension(instance, out KhrSurface khrsf);
        var queueFamiliyIndicies = new QueueFamilyIndiciesDetails(api.vk, surface, gpu, khrsf);
        CommandPoolCreateInfo poolInfo = new()
        {
            QueueFamilyIndex = queueFamiliyIndicies.graphicsFamily,
        };
        _ = api.vk.CreateCommandPool(device, poolInfo, null, out var commandPool);
        return commandPool;
    }

    public static unsafe CommandPool CreateCommandPool(RenderBase data)
    {
        CommandPoolCreateInfo poolInfo = new()
        {
            QueueFamilyIndex = data.indiciesDetails.graphicsFamily,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
        };
        _ = data.vk.CreateCommandPool(data.device, poolInfo, null, out var commandPool);
        return commandPool;
    }

    public static unsafe void DrawFrame(Api api, Device device, SwapchainKHR swapChain, KhrSwapchain khrSwapChain, CommandBuffer[] commandBuffers,
        Fence[] inFlightFences, Fence[] imagesInFlight, Semaphore[] imageAvailableSemaphores, Semaphore[] renderFinishedSemaphores,
        Queue graphicsQueue, Queue presentQueue, ref int currentFrame)
    {
        api.vk.WaitForFences(device, 1, inFlightFences[currentFrame], true, ulong.MaxValue);

        uint imageIndex = 0;
        khrSwapChain.AcquireNextImage(device, swapChain, ulong.MaxValue, imageAvailableSemaphores[currentFrame], default, ref imageIndex);
        if (imagesInFlight[imageIndex].Handle != default)
            api.vk.WaitForFences(device, 1, imagesInFlight[imageIndex], true, ulong.MaxValue);
        imagesInFlight[imageIndex] = inFlightFences[currentFrame];

        var waitSemaphores = stackalloc[] { imageAvailableSemaphores[currentFrame] };
        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };
        var signalSemaphores = stackalloc[] { renderFinishedSemaphores[currentFrame] };

        var buffer = commandBuffers[imageIndex];
        SubmitInfo submitInfo = new()
        {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };

        api.vk.ResetFences(device, 1, inFlightFences[currentFrame]);

        _ = api.vk.QueueSubmit(graphicsQueue, 1, submitInfo, inFlightFences[currentFrame]);
        var swapChains = stackalloc[] { swapChain };
        PresentInfoKHR presentInfo = new()
        {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,
            SwapchainCount = 1,
            PSwapchains = swapChains,
            PImageIndices = &imageIndex
        };

        khrSwapChain.QueuePresent(presentQueue, presentInfo);

        currentFrame = (currentFrame + 1) % inFlightFences.Length;
    }

    public static unsafe CommandBuffer[] CreateCommandBuffers(Api api, ReadOnlySpan<Framebuffer> swapChainFramebuffers, CommandPool commandPool, Device device, RenderPass renderPass, Extent2D swapChainExtent, Pipeline? graphicsPipeline)
    {
        var commandBuffers = new CommandBuffer[swapChainFramebuffers.Length];
        CommandBufferAllocateInfo allocInfo = new()
        {
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)commandBuffers.Length,
        };
        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
            _ = api.vk.AllocateCommandBuffers(device, allocInfo, commandBuffersPtr);

        for (int i = 0; i < commandBuffers.Length; i++)
        {
            CommandBufferBeginInfo beginInfo = new();
            _ = api.vk.BeginCommandBuffer(commandBuffers[i], beginInfo);

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = renderPass,
                Framebuffer = swapChainFramebuffers[i],
                RenderArea = new(extent: swapChainExtent)
            };
            ClearValue clearColor = new()
            {
                Color = { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },
            };

            renderPassInfo.ClearValueCount = 1;
            renderPassInfo.PClearValues = &clearColor;

            api.vk.CmdBeginRenderPass(commandBuffers[i], &renderPassInfo, SubpassContents.Inline);
            if (graphicsPipeline.HasValue)
                api.vk.CmdBindPipeline(commandBuffers[i], PipelineBindPoint.Graphics, graphicsPipeline.Value);
            if (graphicsPipeline.HasValue)
                api.vk.CmdDraw(commandBuffers[i], 3, 1, 0, 0); // Actual draw
            api.vk.CmdEndRenderPass(commandBuffers[i]);
            _ = api.vk.EndCommandBuffer(commandBuffers[i]);
        }
        return commandBuffers;
    }


    internal static unsafe CommandBuffer[] CreateCommandBuffers(RenderBase data, SwapChain sw, CommandPool commandPool, RenderPass renderPass, Extent2D swapChainExtent, Pipeline? graphicsPipeline)
    {
        var commandBuffers = new CommandBuffer[sw.frameBuffers.Length];
        CommandBufferAllocateInfo allocInfo = new()
        {
            CommandPool = commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)commandBuffers.Length,
        };
        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
            _ = data.vk.AllocateCommandBuffers(data.device, allocInfo, commandBuffersPtr);

        ClearValue clearColor = new()
        {
            Color = { Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },
        };

        for (int i = 0; i < commandBuffers.Length; i++)
        {
            CommandBufferBeginInfo beginInfo = new();
            _ = data.vk.BeginCommandBuffer(commandBuffers[i], beginInfo);

            RenderPassBeginInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                RenderPass = renderPass,
                Framebuffer = sw.frameBuffers[i],
                RenderArea = new(extent: swapChainExtent),
                ClearValueCount = 1,
                PClearValues = &clearColor
            };

            data.vk.CmdBeginRenderPass(commandBuffers[i], &renderPassInfo, SubpassContents.Inline);
            if (graphicsPipeline.HasValue)
                data.vk.CmdBindPipeline(commandBuffers[i], PipelineBindPoint.Graphics, graphicsPipeline.Value);
            if (graphicsPipeline.HasValue)
                data.vk.CmdDraw(commandBuffers[i], 3, 1, 0, 0); // Actual draw
            data.vk.CmdEndRenderPass(commandBuffers[i]);
            _ = data.vk.EndCommandBuffer(commandBuffers[i]);
        }
        return commandBuffers;
    }



    public static unsafe PhysicalDevice PickPhysicalDevice(Vk vk, Instance instance, SurfaceKHR surface, KhrSurface khrsf, string[] neededExtensions)
    {
        uint devicedCount = 0;
        _ = vk.EnumeratePhysicalDevices(instance, &devicedCount, null);
        _ = devicedCount != 0;
        Span<PhysicalDevice> devices = stackalloc PhysicalDevice[(int)devicedCount];
        vk.EnumeratePhysicalDevices(instance, &devicedCount, devices);
        PhysicalDevice physicalDevice = default;
        foreach (var device in devices)
        {
            if (IsDeviceSuitable(vk, device, surface, khrsf, neededExtensions))
                return device;
        }
        _ = physicalDevice.Handle != 0;
        return physicalDevice;
    }

    public static unsafe ImmutableArray<ImageView> CreateImageViews(Api api, Device device, ReadOnlySpan<Image> swapChainImages, Format swapchainImageFormat)
    {
        Span<ImageView> swapChainImageViews = stackalloc ImageView[swapChainImages.Length];
        for (int i = 0; i < swapChainImages.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                Image = swapChainImages[i],
                ViewType = ImageViewType.Type2D,
                Format = swapchainImageFormat,
                Components = default,
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
        return ImmutableArray.Create(swapChainImageViews);
    }


    public static unsafe (Device device, Queue graphicsQueue, Queue presentQueue) CreateLogicalDevice(Vk vk, PhysicalDevice gpu, SurfaceKHR surface, KhrSurface khrsf, string[] deviceExtensions)
    {
        var indices = new QueueFamilyIndiciesDetails(vk, surface, gpu, khrsf);
        bool both = indices.graphicsFamily != indices.presentFamily;
        int count = both ? 2 : 1;
        var uniqueQueueFam = stackalloc DeviceQueueCreateInfo[count];
        float queuePriority = 1.0f;
        uniqueQueueFam[0] = new()
        {
            QueueFamilyIndex = indices.graphicsFamily,
            QueueCount = 1,
            PQueuePriorities = &queuePriority
        };
        if (both)
            uniqueQueueFam[1] = new()
            {
                QueueFamilyIndex = indices.presentFamily,
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };

        PhysicalDeviceFeatures deviceFeatures = new();
        DeviceCreateInfo createInfo = new()
        {
            QueueCreateInfoCount = (uint)count,
            PQueueCreateInfos = uniqueQueueFam,

            PEnabledFeatures = &deviceFeatures,

            EnabledExtensionCount = (uint)deviceExtensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(deviceExtensions),
            EnabledLayerCount = 0
        };
        _ = vk.CreateDevice(gpu, &createInfo, null, out var device);

        vk.GetDeviceQueue(device, indices.graphicsFamily, 0, out var graphicsQueue);
        vk.GetDeviceQueue(device, indices.presentFamily, 0, out var presentQueue);

        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
        return (device, graphicsQueue, presentQueue);
    }


    public static unsafe (SwapchainKHR sw, Image[] swImages, Format swImFormat, Extent2D extent)
        CreateSwapChain(Api api, Window* window, Instance instance, Device device, PhysicalDevice physicalDevice, SurfaceKHR surface)
    {
        api.vk.TryGetInstanceExtension(instance, out KhrSurface khrsf);
        var swapChainSupport = new SwapChainSupportDetails(physicalDevice, surface, khrsf);

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

        var indices = new QueueFamilyIndiciesDetails(api.vk, surface, physicalDevice, khrsf);
        var queueFamilyIndices = stackalloc[] { indices.graphicsFamily, indices.presentFamily };

        if (indices.graphicsFamily != indices.presentFamily)
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

        return (sw, swImages, surfaceFormat.Format, extent);
    }


    public static SurfaceFormatKHR ChooseSwapSurfaceFormat(ImmutableArray<SurfaceFormatKHR> formats)
    {
        return formats.AsSpan().Exist(f => f.Format == Format.B8G8R8A8Srgb && f.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr, out var format) ?
            format : formats[0];
    }

    public static PresentModeKHR ChoosePresentMode(ImmutableArray<PresentModeKHR> availablePresentModes)
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
        int w, h;
        w = h = 0;
        api.sdl.VulkanGetDrawableSize(window, ref w, ref h);
        return ChooseSwapExtent(w, h, capabilities);
    }

    public static unsafe Extent2D ChooseSwapExtent(int width, int height, SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
            return capabilities.CurrentExtent;
        return new()
        {
            Width = Math.Clamp((uint)width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
            Height = Math.Clamp((uint)height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height)
        };
    }

    private static unsafe bool IsDeviceSuitable(Vk vk, PhysicalDevice device, SurfaceKHR surface, KhrSurface khrsf, string[] neededExtensions)
    {
        var indices = new QueueFamilyIndiciesDetails(vk, surface, device, khrsf);
        bool extensionsSupported = CheckDeviceExtensionsSupport(vk, device, neededExtensions);
        return indices.suitable && extensionsSupported && SwapChainSupportDetails.Adequate(device, surface, khrsf);
    }

    private static unsafe bool CheckDeviceExtensionsSupport(Vk vk, PhysicalDevice device, string[] neededExtensions)
    {
        uint extentionsCount = 0;
        vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extentionsCount, null);
        Span<ExtensionProperties> availableExtensions = stackalloc ExtensionProperties[(int)extentionsCount];
        vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extentionsCount, availableExtensions);

        foreach (var needed in neededExtensions)
            if (!availableExtensions.Exist(present => needed == Marshal.PtrToStringAnsi((nint)present.ExtensionName)))
                return false;
        return true;
    }

}
