﻿using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static DeltaEngine.ThrowHelper;
using Buffer = Silk.NET.Vulkan.Buffer;
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

    public static unsafe Instance CreateVkInstance(Vk vk, string app, string engine, string[] extensions, string[] layers, void* instanceChain)
    {
        ApplicationInfo appInfo = new()
        {
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi(engine),
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(app),
            EngineVersion = new Version32(1, 0, 0),
            ApplicationVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version10
        };
        InstanceCreateInfo createInfo = new()
        {
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = (uint)extensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions),
            PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(layers),
            EnabledLayerCount = (uint)layers.Length,
            PNext = instanceChain,
        };

        _ = vk.CreateInstance(createInfo, null, out var instance);
        Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
        Marshal.FreeHGlobal((nint)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
        SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
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

        AttachmentReference colorAttachmentRef = new(0, ImageLayout.ColorAttachmentOptimal);

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

    public static unsafe (Buffer buffer, DeviceMemory memory) CreateBuffer(RenderBase data, ulong size, BufferUsageFlags bufferUsageFlags, MemoryPropertyFlags memoryPropertyFlags)
    {
        BufferCreateInfo createInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = bufferUsageFlags,
            SharingMode = SharingMode.Exclusive,
            Flags = default,
        };
        _ = data.vk.CreateBuffer(data.device, createInfo, null, out var buffer);
        var reqs = data.vk.GetBufferMemoryRequirements(data.device, buffer);
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = (uint)FindMemoryType(data, (int)reqs.MemoryTypeBits, memoryPropertyFlags)
        };
        _ = data.vk.AllocateMemory(data.device, allocateInfo, null, out var memory);
        _ = data.vk.BindBufferMemory(data.device, buffer, memory, 0);
        return (buffer, memory);
    }

    public static unsafe (Buffer, DeviceMemory) CreateVertexBuffer(RenderBase data, Vertex[] vertices)
    {
        var size = (uint)(Vertex.Size * vertices.Length);
        var res = CreateBuffer(data, size, BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        void* datap;
        data.vk.MapMemory(data.device, res.memory, 0, size, 0, &datap);

        Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(datap),
            ref Unsafe.As<Vertex, byte>(ref MemoryMarshal.GetArrayDataReference(vertices)),
            size);

        data.vk.UnmapMemory(data.device, res.memory);
        return res;
    }

    public static unsafe(Buffer, DeviceMemory) CreateIndexBuffer(RenderBase data, uint[] indices)
    {
        uint size = (uint)(sizeof(uint) * indices.Length);
        var res = CreateBuffer(data, size, BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        void* datap;
        data.vk.MapMemory(data.device, res.memory, 0, size, 0, &datap);

        Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(datap),
            ref Unsafe.As<uint, byte>(ref MemoryMarshal.GetArrayDataReference(indices)),
            size);

        data.vk.UnmapMemory(data.device, res.memory);
        return res;
    }

    public static unsafe void CopyBuffer(RenderBase data, Buffer source, Buffer destination, ulong sourceoffset, ulong destinationOffset, ulong size)
    {
        CommandBufferAllocateInfo allocateInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandBufferCount = 1,
            CommandPool = data.commandPool,
            Level = CommandBufferLevel.Primary,
            PNext = null
        };
        data.vk.AllocateCommandBuffers(data.device, &allocateInfo, out var cmdbuffer);
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
        };
        data.vk.BeginCommandBuffer(cmdbuffer, beginInfo);
        BufferCopy copyRegion = new()
        {
            SrcOffset = sourceoffset,
            DstOffset = destinationOffset,
            Size = size,
        };
        data.vk.CmdCopyBuffer(cmdbuffer, source, destination, 1, &copyRegion);
        data.vk.EndCommandBuffer(cmdbuffer);
    }

    public static int FindMemoryType(RenderBase data, int typeFilter, MemoryPropertyFlags properties)
    {
        int i = 0;
        for (; i < data.gpuMemory.memoryProperties.MemoryTypeCount; i++)
            if (Convert.ToBoolean(typeFilter & (1 << i)) && (data.gpuMemory.memoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties)
                return i;
        _ = false;
        return i;
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

        var bind = Vertex.GetBindingDescription(vertShader.attributeMask);
        var attr = stackalloc VertexInputAttributeDescription[vertShader.attributeMask.GetAttributesCount()];
        Vertex.FillAttributeDesctiption(attr, vertShader.attributeMask);
        PipelineVertexInputStateCreateInfo vertexInputInfo = new()
        {
            VertexBindingDescriptionCount = 1,
            PVertexBindingDescriptions = &bind,
            VertexAttributeDescriptionCount = (uint)Vertex.AttributeDesctiption.Length,
            PVertexAttributeDescriptions = attr
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
            SType = StructureType.GraphicsPipelineCreateInfo,
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


    internal static unsafe CommandBuffer[] CreateCommandBuffers(RenderBase data, int buffersCount)
    {
        var commandBuffers = new CommandBuffer[buffersCount];
        CommandBufferAllocateInfo allocInfo = new()
        {
            CommandPool = data.commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = (uint)buffersCount,
        };
        fixed (CommandBuffer* commandBuffersPtr = commandBuffers)
            _ = data.vk.AllocateCommandBuffers(data.device, allocInfo, commandBuffersPtr);
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
        return ChooseSwapSurfaceFormat(formats, new(Format.B8G8R8A8Srgb, ColorSpaceKHR.SpaceSrgbNonlinearKhr));
    }


    public static SurfaceFormatKHR ChooseSwapSurfaceFormat(ImmutableArray<SurfaceFormatKHR> formats, SurfaceFormatKHR targetFormat)
    {
        return formats.AsSpan().Exist(f => f.Format == targetFormat.Format && f.ColorSpace == targetFormat.ColorSpace, out var format) ?
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