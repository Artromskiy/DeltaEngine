using Delta.Assets;
using Delta.Rendering.Collections;
using Delta.Rendering.Internal;
using Delta.Runtime;
using Delta.Utilities;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Delta.Rendering;

internal static unsafe class RenderHelper
{
    private const uint flags = (uint)
    (
          WindowFlags.Vulkan
        | WindowFlags.Shown
        | WindowFlags.Resizable
        | WindowFlags.AllowHighdpi
        | WindowFlags.SkipTaskbar
   );

    public static Window* CreateWindow(Sdl sdl, string title)
    {
        return sdl.CreateWindow(Encoding.UTF8.GetBytes(title), 100, 100, 1000, 1000, flags);
    }

    public static Instance CreateVkInstance(Vk vk, string app, string engine,
        ReadOnlySpan<string> extensions, ReadOnlySpan<string> layers, void* instanceChain)
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
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledExtensionCount = (uint)extensions.Length,
            PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions.ToArray()),
            PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(layers.ToArray()),
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

    public static Fence CreateFence(Vk vk, DeviceQueues deviceQ, bool signaled)
    {
        FenceCreateFlags flag = signaled ? FenceCreateFlags.SignaledBit : FenceCreateFlags.None;
        FenceCreateInfo fenceCreate = new(StructureType.FenceCreateInfo, null, flag);
        vk.CreateFence(deviceQ, fenceCreate, null, out var result);
        return result;
    }

    public static Semaphore CreateSemaphore(Vk vk, DeviceQueues deviceQ)
    {
        SemaphoreCreateInfo semaphoreCreate = new(StructureType.SemaphoreCreateInfo);
        _ = vk.CreateSemaphore(deviceQ, semaphoreCreate, null, out var result);
        return result;
    }

    public static void BeginCmdBuffer(Vk vk, CommandBuffer cmdBuffer)
    {
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _ = vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
    }

    public static void CopyCmd<T>(Vk vk, GpuArray<T> source, DynamicBuffer destination, CommandBuffer cmdBuffer) where T : unmanaged
    {
        destination.EnsureSize(source.Size);
        BufferCopy copy = new(0, 0, (ulong)Math.Min(source.Size, destination.Size));
        vk.CmdCopyBuffer(cmdBuffer, source.Buffer, destination.GetBuffer(), 1, &copy);
    }

    public static void CopyCmd(Vk vk, GpuByteArray source, DynamicBuffer destination, CommandBuffer cmdBuffer)
    {
        destination.EnsureSize(source.Size);
        BufferCopy copy = new(0, 0, (ulong)Math.Min(source.Size, destination.Size));
        vk.CmdCopyBuffer(cmdBuffer, source.Buffer, destination.GetBuffer(), 1, &copy);
    }

    public static void EndCmdBuffer(Vk vk, Queue queue, CommandBuffer cmdBuffer, Fence fence, Semaphore semaphore)
    {
        _ = vk.EndCommandBuffer(cmdBuffer);
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &semaphore,
        };
        _ = vk.QueueSubmit(queue, 1, &submitInfo, fence);
    }

    public static RenderPass CreateRenderPass(Vk vk, Device device, Format imageFormat, ImageLayout finalLayout)
    {
        AttachmentDescription colorAttachment = new()
        {
            Format = imageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = finalLayout,
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
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &dependency,
        };

        _ = vk.CreateRenderPass(device, renderPassInfo, null, out var renderPass);
        return renderPass;
    }

    public static string[] GetSdlVulkanExtensions(Sdl sdl, Window* window)
    {
        uint extCount = 0;
        _ = sdl.VulkanGetInstanceExtensions(window, &extCount, (byte**)null);
        string[] extensions = new string[extCount];
        _ = sdl.VulkanGetInstanceExtensions(window, &extCount, extensions);
        return extensions;
    }

    public static SurfaceKHR CreateSurface(Sdl sdl, Window* window, Instance instance)
    {
        var nondispatchable = new VkNonDispatchableHandle();
        _ = sdl.VulkanCreateSurface(window, instance.ToHandle(), ref nondispatchable);
        return nondispatchable.ToSurface();
    }

    public static string[] GetSilkVulkanExtensions(IVkSurface vkSurface)
    {
        nint extensionsPtr = new(vkSurface.GetRequiredExtensions(out var count));
        string[] extensions = new string[count];
        SilkMarshal.CopyPtrToStringArray(extensionsPtr, extensions);
        return extensions;
    }

    public static (Buffer buffer, DeviceMemory memory) CreateBufferAndMemory(Vk vk, DeviceQueues deviceQ, ulong size, BufferUsageFlags bufferUsageFlags, MemoryPropertyFlags memoryPropertyFlags)
    {
        BufferCreateInfo createInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = bufferUsageFlags,
            SharingMode = SharingMode.Exclusive,
            Flags = default,
        };
        _ = vk.CreateBuffer(deviceQ, createInfo, null, out var buffer);
        var reqs = vk.GetBufferMemoryRequirements(deviceQ, buffer);
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = deviceQ.gpu.FindMemoryType(reqs.MemoryTypeBits, memoryPropertyFlags)
        };
        _ = vk.AllocateMemory(deviceQ, allocateInfo, null, out var memory);
        _ = vk.BindBufferMemory(deviceQ, buffer, memory, 0);
        return (buffer, memory);
    }

    public static (Buffer buffer, DeviceMemory memory) CreateVertexBuffer(Vk vk, DeviceQueues deviceQ, MeshData meshData, VertexAttribute attributeMask)
    {
        var size = (uint)(attributeMask.GetVertexSize() * meshData.vertexCount);
        var res = CreateBufferAndMemory(vk, deviceQ, size, BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        void* datap;
        vk.MapMemory(deviceQ, res.memory, 0, size, 0, &datap);

        Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(datap),
            ref MemoryMarshal.GetArrayDataReference(MeshCollection.GetMeshVariant(meshData, attributeMask)),
            size);

        vk.UnmapMemory(deviceQ, res.memory);
        return res;
    }
    public static (Buffer buffer, DeviceMemory memory) CreateIndexBuffer(Vk vk, DeviceQueues deviceQ, MeshData meshData)
    {
        var size = (uint)(sizeof(uint) * meshData.GetIndices().Length);
        var res = CreateBufferAndMemory(vk, deviceQ, size, BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        void* datap;
        vk.MapMemory(deviceQ, res.memory, 0, size, 0, &datap);

        Span<uint> dataSpan = new(datap, meshData.GetIndices().Length);
        meshData.GetIndices().CopyTo(dataSpan);

        vk.UnmapMemory(deviceQ, res.memory);
        return res;
    }

    public static VertexInputBindingDescription GetBindingDescription(VertexAttribute vertexAttributeMask) => new()
    {
        Binding = 0,
        InputRate = VertexInputRate.Vertex,
        Stride = (uint)vertexAttributeMask.GetVertexSize()
    };

    public static void FillAttributeDesctiption(Span<VertexInputAttributeDescription> description, VertexAttribute vertexAttributeMask)
    {
        int index = 0;
        int offset = 0;
        foreach (var attrib in vertexAttributeMask.Iterate())
        {
            Format format = attrib.size switch
            {
                4 * 1 => Format.R32Sfloat,
                4 * 2 => Format.R32G32Sfloat,
                4 * 3 => Format.R32G32B32Sfloat,
                4 * 4 => Format.R32G32B32A32Sfloat,
                _ => throw new Exception("Invalid vertex attribute size defined")
            };
            description[index++] = new()
            {
                Binding = 0,
                Format = format,
                Location = (uint)attrib.location,
                Offset = (uint)offset,
            };
            offset += attrib.size;
        }
    }

    public static VertexAttribute GetInputAttributes(ReadOnlySpan<byte> shaderCode)
    {
        Context* context = default;
        ParsedIr* ir = default;
        Compiler* compiler = default;
        Resources* resources = default;
        ReflectedResource* list = default;
        Set set = default;
        nuint count;
        nuint i;
        VertexAttribute res = default;

        using Cross api = Cross.GetApi();
        api.ContextCreate(&context);

        fixed (void* decodedPtr = shaderCode)
        {
            api.ContextParseSpirv(context, (uint*)decodedPtr, (uint)shaderCode.Length / 4, &ir);
            api.ContextCreateCompiler(context, Backend.None, ir, CaptureMode.TakeOwnership, &compiler);
            api.CompilerGetActiveInterfaceVariables(compiler, &set);
            api.CompilerCreateShaderResources(compiler, &resources);
            api.ResourcesGetResourceListForType(resources, ResourceType.StageInput, &list, &count);
            for (i = 0; i < count; i++)
            {
                var loc = (int)api.CompilerGetDecoration(compiler, list[i].Id, Decoration.Location);
                res |= (VertexAttribute)(1 << loc);
                var binding = api.CompilerGetDecoration(compiler, list[i].Id, Decoration.Binding);
                var dset = api.CompilerGetDecoration(compiler, list[i].Id, Decoration.DescriptorSet);
            }
        }
        api.ContextDestroy(context);
        return res;
    }

    public static Pipeline CreateGraphicsPipeline(Vk vk, DeviceQueues deviceQ, PipelineLayout pipelineLayout, RenderPass renderPass, ShaderData shaderData, out VertexAttribute attributeMask)
    {
        using var vertShader = new PipelineShader(vk, deviceQ, ShaderStageFlags.VertexBit, shaderData.GetVertBytes());
        using var fragShader = new PipelineShader(vk, deviceQ, ShaderStageFlags.FragmentBit, shaderData.GetFragBytes());
        var stages = stackalloc PipelineShaderStageCreateInfo[2]
        {
            ShaderModuleGroupCreator.Create(vertShader),
            ShaderModuleGroupCreator.Create(fragShader)
        };
        attributeMask = GetInputAttributes(shaderData.GetVertBytes());
        var bind = GetBindingDescription(attributeMask);
        var attribCount = attributeMask.GetAttributesCount();
        var attr = stackalloc VertexInputAttributeDescription[attribCount];
        Span<VertexInputAttributeDescription> attrSpan = new(attr, attribCount);
        FillAttributeDesctiption(attrSpan, attributeMask);
        PipelineVertexInputStateCreateInfo vertexInputInfo = new()
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 1,
            PVertexBindingDescriptions = &bind,
            VertexAttributeDescriptionCount = (uint)attribCount,
            PVertexAttributeDescriptions = attr,
        };

        PipelineInputAssemblyStateCreateInfo inputAssembly = new()
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false,
        };

        var dynamicStates = stackalloc[] { DynamicState.Viewport, DynamicState.Scissor };
        PipelineDynamicStateCreateInfo dynamicState = new()
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            PDynamicStates = dynamicStates,
            DynamicStateCount = 2,
        };
        PipelineViewportStateCreateInfo viewportState = new()
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1,
        };

        PipelineRasterizationStateCreateInfo rasterizer = new()
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
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
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
        };

        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false,
        };
        //PipelineDepthStencilStateCreateInfo depthStencil = new()
        //{
        //    SType = StructureType.PipelineDepthStencilStateCreateInfo,
        //    DepthTestEnable = true,
        //    DepthWriteEnable = true,
        //    DepthCompareOp = CompareOp.LessOrEqual,
        //    DepthBoundsTestEnable = false
        //};

        PipelineColorBlendStateCreateInfo colorBlending = new()
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment,
        };

        GraphicsPipelineCreateInfo pipelineInfo = new()
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = stages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PDynamicState = &dynamicState,
            //PDepthStencilState = &depthStencil,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PColorBlendState = &colorBlending,
            Layout = pipelineLayout,
            RenderPass = renderPass,
            Subpass = 0,
            BasePipelineHandle = default,
        };
        _ = vk.CreateGraphicsPipelines(deviceQ, default, 1, pipelineInfo, null, out var graphicsPipeline);
        return graphicsPipeline;
    }


    public static DescriptorPool CreateDescriptorPool(Vk vk, DeviceQueues deviceQ)
    {
        uint descriptorCount = 50;
        var poolSizes = stackalloc DescriptorPoolSize[]
        {
            new DescriptorPoolSize(DescriptorType.StorageBuffer, descriptorCount),
        };
        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = poolSizes,
            MaxSets = descriptorCount,
        };
        _ = vk.CreateDescriptorPool(deviceQ, poolInfo, null, out var result);
        return result;
    }

    public static void UpdateDescriptorSets(Vk vk, DeviceQueues deviceQ, DescriptorSet descriptorSet, Buffer buffer, uint binding, DescriptorType bufferUsage)
    {
        DescriptorBufferInfo bufferInfo = new()
        {
            Buffer = buffer,
            Range = Vk.WholeSize,
        };
        WriteDescriptorSet descriptorWrite = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorType = bufferUsage,
            DstSet = descriptorSet,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo,
        };
        vk.UpdateDescriptorSets(deviceQ, 1, descriptorWrite, 0, null);
    }

    internal static CommandBuffer CreateCommandBuffer(Vk vk, DeviceQueues deviceQ, CommandPool cmdPool)
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = cmdPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        CommandBuffer commandBuffer;
        _ = vk.AllocateCommandBuffers(deviceQ, allocInfo, &commandBuffer);
        return commandBuffer;
    }


    public static Gpu PickPhysicalDevice(Vk vk, Instance instance, Func<PhysicalDevice, int> prioritizedSelector)
    {
        uint deviceCount = 0;
        _ = vk.EnumeratePhysicalDevices(instance, &deviceCount, null);
        Span<PhysicalDevice> devices = stackalloc PhysicalDevice[(int)deviceCount];
        vk.EnumeratePhysicalDevices(instance, &deviceCount, devices);
        PhysicalDevice selected = default;
        int currentPriority = 0;
        foreach (var device in devices)
        {
            var priority = prioritizedSelector(device);
            if (priority > currentPriority)
            {
                currentPriority = priority;
                selected = device;
            }
        }
        _ = currentPriority != 0;
        return new Gpu(vk, selected);
    }

    public static void CopyImage(Vk vk, CommandBuffer cmdBuffer, DeviceQueues deviceQ,
        Image source, Buffer destionation, int width, int height,
        Semaphore waitSemaphore, Fence signalFence)
    {
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        BufferImageCopy copy = new()
        {
            BufferImageHeight = (uint)height,
            BufferRowLength = (uint)width,
            BufferOffset = 0,
            ImageExtent =
            {
                Width = (uint)width,
                Height = (uint)height,
                Depth = 1
            },
            ImageOffset =
            {
                X = 0,
                Y = 0,
                Z = 0
            },
            ImageSubresource =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LayerCount = 1,
                MipLevel = 0,
                BaseArrayLayer = 0
            }
        };
        var waitStageFlags = PipelineStageFlags.TransferBit;
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
            PWaitSemaphores = &waitSemaphore,
            WaitSemaphoreCount = 1,
            PWaitDstStageMask = &waitStageFlags
        };
        _ = vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
        vk.CmdCopyImageToBuffer(cmdBuffer, source, ImageLayout.TransferSrcOptimal, destionation, 1, copy);
        _ = vk.EndCommandBuffer(cmdBuffer);
        _ = vk.QueueSubmit(deviceQ.GetQueue(QueueType.Transfer), 1, &submitInfo, signalFence);
    }


    public static void CopyImage(Vk vk, CommandBuffer cmdBuffer, DeviceQueues deviceQ,
        Image source, Image destionation, int width, int height,
        Semaphore waitSemaphore, Fence signalFence)
    {
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _ = vk.BeginCommandBuffer(cmdBuffer, &beginInfo);

        InsertImageMemoryBarrier(vk, cmdBuffer,
            destionation,
            0,
            AccessFlags.TransferWriteBit,
            ImageLayout.Undefined,
            ImageLayout.TransferDstOptimal,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.TransferBit,
            new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1));

        ImageCopy copy = new()
        {
            SrcSubresource = new(ImageAspectFlags.ColorBit, layerCount: 1),
            DstSubresource = new(ImageAspectFlags.ColorBit, layerCount: 1),
            Extent = new((uint)width, (uint)height, 1),
        };

        vk.CmdCopyImage(cmdBuffer,
            source, ImageLayout.TransferSrcOptimal,
            destionation, ImageLayout.TransferDstOptimal,
            1, copy);

        InsertImageMemoryBarrier(vk, cmdBuffer,
            destionation,
            AccessFlags.TransferWriteBit,
            AccessFlags.TransferReadBit,
            ImageLayout.TransferDstOptimal,
            ImageLayout.General,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.TransferBit,
            new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1));

        _ = vk.EndCommandBuffer(cmdBuffer);
        var waitStageFlags = PipelineStageFlags.TransferBit;
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
            PWaitSemaphores = &waitSemaphore,
            WaitSemaphoreCount = 1,
            PWaitDstStageMask = &waitStageFlags
        };
        _ = vk.QueueSubmit(deviceQ.GetQueue(QueueType.Transfer), 1, &submitInfo, signalFence);
    }



    public static (Image image, DeviceMemory memory) CreateImage(Vk vk, DeviceQueues deviceQ,
        int width, int height, Format imageFormat, ImageUsageFlags usageFlags, MemoryPropertyFlags memoryFlags,
        ImageTiling imageTiling)
    {
        ImageCreateInfo createInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = imageFormat,
            Extent =
                {
                    Width= (uint)width,
                    Height = (uint)height,
                    Depth = 1,
                },
            InitialLayout = ImageLayout.Undefined,
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = imageTiling,
            Usage = usageFlags
        };
        _ = vk.CreateImage(deviceQ, createInfo, null, out var image);
        var reqs = vk.GetImageMemoryRequirements(deviceQ, image);
        var memTypeIndex = deviceQ.gpu.FindMemoryType(reqs.MemoryTypeBits, memoryFlags);
        MemoryAllocateInfo memAlloc = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = memTypeIndex,
        };
        _ = vk.AllocateMemory(deviceQ, memAlloc, null, out var memory);
        _ = vk.BindImageMemory(deviceQ, image, memory, 0);
        return (image, memory);
    }


    public static (ImmutableArray<Image> images, ImmutableArray<DeviceMemory> imagesMemory) CreateImages(Vk vk, DeviceQueues deviceQ,
        int imagesCount, int width, int height, Format imageFormat, ImageUsageFlags usageFlags, MemoryPropertyFlags memoryFlags,
        ImageTiling imageTiling)
    {
        Span<Image> images = stackalloc Image[imagesCount];
        Span<DeviceMemory> imagesMemory = stackalloc DeviceMemory[imagesCount];

        for (int i = 0; i < images.Length; i++)
            (images[i], imagesMemory[i]) = CreateImage(vk, deviceQ, width, height, imageFormat, usageFlags, memoryFlags, imageTiling);

        return (ImmutableArray.Create(images), ImmutableArray.Create(imagesMemory));
    }

    public static ImmutableArray<ImageView> CreateImageViews(Vk vk, Device device, ReadOnlySpan<Image> images, Format imageFormat)
    {
        Span<ImageView> imageViews = stackalloc ImageView[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = images[i],
                ViewType = ImageViewType.Type2D,
                Format = imageFormat,
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
            _ = vk.CreateImageView(device, createInfo, null, out imageViews[i]);
        }
        return ImmutableArray.Create(imageViews);
    }

    public static unsafe DescriptorSetLayout CreateDescriptorSetLayout(ReadOnlySpan<int> bindingsArray)
    {
        var renderData = IRuntimeContext.Current.GraphicsModule.RenderData;
        var vk = renderData.vk;
        var deviceQ = renderData.deviceQ;
        const ShaderStageFlags StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit;
        var bindings = stackalloc DescriptorSetLayoutBinding[bindingsArray.Length];
        for (int i = 0; i < bindingsArray.Length; i++)
            bindings[i] = new(checked((uint)bindingsArray[i]), DescriptorType.StorageBuffer, 1, StageFlags);
        DescriptorSetLayoutCreateInfo createInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = (uint)bindingsArray.Length,
            PBindings = bindings,
        };
        _ = vk.CreateDescriptorSetLayout(deviceQ, &createInfo, null, out DescriptorSetLayout setLayout);
        return setLayout;
    }

    public unsafe static PipelineLayout CreatePipelineLayout(ReadOnlySpan<DescriptorSetLayout> layouts)
    {
        var renderData = IRuntimeContext.Current.GraphicsModule.RenderData;
        var vk = renderData.vk;
        var deviceQ = renderData.deviceQ;
        PipelineLayout result;
        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            PipelineLayoutCreateInfo pipelineLayoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint)layouts.Length,
                PSetLayouts = layoutsPtr,
            };
            _ = vk.CreatePipelineLayout(deviceQ, pipelineLayoutInfo, null, out result);
        }
        return result;
    }

    public static ImmutableArray<Framebuffer> CreateFramebuffers(Vk vk, Device device, ReadOnlySpan<ImageView> imageViews, RenderPass renderPass, int width, int height)
    {
        Span<Framebuffer> framebuffers = stackalloc Framebuffer[imageViews.Length];
        for (int i = 0; i < imageViews.Length; i++)
        {
            var attachment = imageViews[i];
            FramebufferCreateInfo framebufferInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = renderPass,
                AttachmentCount = 1,
                PAttachments = &attachment,
                Width = (uint)width,
                Height = (uint)height,
                Layers = 1,
            };
            _ = vk.CreateFramebuffer(device, framebufferInfo, null, out framebuffers[i]);
        }
        return ImmutableArray.Create(framebuffers);
    }

    internal static DeviceQueues CreateLogicalDevice(Vk vk, Gpu gpu, ReadOnlySpan<QueueType> neededQueues, ReadOnlySpan<string> deviceExtensions)
    {
        var queueFamilies = SelectQueueFamilies(vk, gpu, neededQueues);
        return new DeviceQueues(vk, gpu, queueFamilies, deviceExtensions);
    }

    private static FamilyQueues SelectQueueFamilies(Vk vk, Gpu gpu, ReadOnlySpan<QueueType> neededQueues)
    {
        uint queueFamilyCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, null);
        Span<QueueFamilyProperties> queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, queueFamilies);
        int length = (int)queueFamilyCount;
        Span<int> maxQueuesCount = stackalloc int[length];
        Span<int> queuesCount = stackalloc int[length];
        Span<QueueFlags> supportedFlags = stackalloc QueueFlags[length];
        var values = neededQueues;
        for (int i = 0; i < length; i++)
        {
            var props = queueFamilies[i];
            maxQueuesCount[i] = (int)props.QueueCount;
            supportedFlags[i] = props.QueueFlags;
        }
        Span<(int family, int queueNum)?> selected = stackalloc (int, int)?[Enums.GetCount<QueueType>()];
        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < length; j++)
            {
                var supported = supportedFlags[j].Supports(values[i]);
                bool empty = queuesCount[j] == 0;
                bool hasFreeSpace = maxQueuesCount[j] > queuesCount[j];
                if (supported && empty && hasFreeSpace)
                {
                    selected[(int)values[i]] = (j, queuesCount[j]++);
                    goto end;
                }
            }

            for (int j = 0; j < length; j++)
            {
                var supported = supportedFlags[j].Supports(values[i]);
                bool hasFreeSpace = maxQueuesCount[j] > queuesCount[j];
                if (supported && hasFreeSpace)
                {
                    selected[(int)values[i]] = (j, queuesCount[j]++);
                    goto end;
                }
            }
        end: { }
        }
        return new FamilyQueues(selected);
    }


    public static SurfaceFormatKHR ChooseSwapSurfaceFormat(ImmutableArray<SurfaceFormatKHR> formats, SurfaceFormatKHR targetFormat)
    {
        return formats.AsSpan().Exist(f => f.Format == targetFormat.Format && f.ColorSpace == targetFormat.ColorSpace, out var format) ?
            format : formats[0];
    }

    public static Extent2D ChooseSwapExtent(int width, int height, SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
            return capabilities.CurrentExtent;
        return new()
        {
            Width = Math.Clamp((uint)width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width),
            Height = Math.Clamp((uint)height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height)
        };
    }

    public static bool IsDeviceSuitable(Vk vk, PhysicalDevice gpu, SurfaceKHR surface, KhrSurface khrsf, ReadOnlySpan<string> neededExtensions)
    {
        uint queueFamilyCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, null);
        Span<QueueFamilyProperties> queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, queueFamilies);
        bool hasPresent = false;
        for (int i = 0; i < queueFamilies.Length; i++)
        {
            _ = khrsf.GetPhysicalDeviceSurfaceSupport(gpu, (uint)i, surface, out var presentSupport);
            if (presentSupport && (hasPresent = true))
                break;
        }
        bool hasGraphics = queueFamilies.Exist(f => f.QueueFlags.HasFlag(QueueFlags.GraphicsBit));
        bool extensionsSupported = CheckDeviceExtensionsSupport(vk, gpu, neededExtensions);
        return hasGraphics && hasPresent && extensionsSupported && SdlRendering.SwapChainSupportDetails.Adequate(gpu, surface, khrsf);
    }

    public static bool IsDeviceSuitable(Vk vk, PhysicalDevice device, ReadOnlySpan<string> neededExtensions)
    {
        var gpu = new Gpu(vk, device);

        uint queueFamilyCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, null);
        Span<QueueFamilyProperties> queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        vk.GetPhysicalDeviceQueueFamilyProperties(gpu, &queueFamilyCount, queueFamilies);

        static bool HasGraphics(QueueFamilyProperties f) => f.QueueFlags.HasFlag(QueueFlags.GraphicsBit);
        bool hasGraphics = queueFamilies.Exist(&HasGraphics);

        bool extensionsSupported = CheckDeviceExtensionsSupport(vk, device, neededExtensions);
        return gpu.HasQueue(QueueType.Graphics) && extensionsSupported;
    }

    private static bool CheckDeviceExtensionsSupport(Vk vk, PhysicalDevice device, ReadOnlySpan<string> neededExtensions)
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

    public static void SetImageLayout
        (
        Vk vk,
        CommandBuffer cmdbuffer,
        Image image,
        ImageLayout oldImageLayout,
        ImageLayout newImageLayout,
        ImageSubresourceRange subresourceRange,
        PipelineStageFlags srcStageMask,
        PipelineStageFlags dstStageMask)
    {
        // Create an image barrier object
        ImageMemoryBarrier imageMemoryBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldImageLayout,
            NewLayout = newImageLayout,
            Image = image,
            SubresourceRange = subresourceRange,
        };

        // Source layouts (old)
        // Source access mask controls actions that have to be finished on the old layout
        // before it will be transitioned to the new layout
        imageMemoryBarrier.SrcAccessMask = oldImageLayout switch
        {
            // Image layout is undefined (or does not matter)
            // Only valid as initial layout
            // No flags required, listed only for completeness
            ImageLayout.Undefined => imageMemoryBarrier.SrcAccessMask = 0,

            // Image is preinitialized
            // Only valid as initial layout for linear images, preserves memory contents
            // Make sure host writes have been finished
            ImageLayout.Preinitialized => AccessFlags.HostWriteBit,

            // Image is a color attachment
            // Make sure any writes to the color buffer have been finished
            ImageLayout.ColorAttachmentOptimal => AccessFlags.ColorAttachmentWriteBit,

            // Image is a depth/stencil attachment
            // Make sure any writes to the depth/stencil buffer have been finished
            ImageLayout.DepthStencilAttachmentOptimal => AccessFlags.DepthStencilAttachmentWriteBit,

            // Image is a transfer source
            // Make sure any reads from the image have been finished
            ImageLayout.TransferSrcOptimal => AccessFlags.TransferReadBit,

            // Image is a transfer destination
            // Make sure any writes to the image have been finished
            ImageLayout.TransferDstOptimal => AccessFlags.TransferWriteBit,

            // Image is read by a shader
            // Make sure any shader reads from the image have been finished
            ImageLayout.ShaderReadOnlyOptimal => AccessFlags.ShaderReadBit,

            // Other source layouts aren't handled (yet)
            _ => throw new NotImplementedException(),
        };

        // Target layouts (new)
        // Destination access mask controls the dependency for the new image layout
        imageMemoryBarrier.DstAccessMask = newImageLayout switch
        {
            // Image will be used as a transfer destination
            // Make sure any writes to the image have been finished
            ImageLayout.TransferDstOptimal => AccessFlags.TransferWriteBit,

            // Image will be used as a transfer source
            // Make sure any reads from the image have been finished
            ImageLayout.TransferSrcOptimal => AccessFlags.TransferReadBit,

            // Image will be used as a color attachment
            // Make sure any writes to the color buffer have been finished
            ImageLayout.ColorAttachmentOptimal => AccessFlags.ColorAttachmentWriteBit,

            // Image layout will be used as a depth/stencil attachment
            // Make sure any writes to depth/stencil buffer have been finished
            ImageLayout.DepthStencilAttachmentOptimal => imageMemoryBarrier.DstAccessMask | AccessFlags.DepthStencilAttachmentWriteBit,

            // Image will be read in a shader (sampler, input attachment)
            // Make sure any writes to the image have been finished
            ImageLayout.ShaderReadOnlyOptimal => ShaderReadOnlyOptimalDstAccessMask(),

            // Other source layouts aren't handled (yet)
            _ => throw new NotImplementedException()
        };

        AccessFlags ShaderReadOnlyOptimalDstAccessMask()
        {
            if (imageMemoryBarrier.SrcAccessMask == 0)
                imageMemoryBarrier.SrcAccessMask = AccessFlags.HostWriteBit | AccessFlags.TransferWriteBit;
            return AccessFlags.ShaderReadBit;
        }

        // Put barrier inside setup command buffer
        vk.CmdPipelineBarrier(
            cmdbuffer,
            srcStageMask,
            dstStageMask,
            0,
            0, null,
            0, null,
            1, imageMemoryBarrier);
    }


    // Fixed sub resource on first mip level and layer
    private static void SetImageLayout(
            Vk vk,
            CommandBuffer cmdbuffer,
            Image image,
            ImageAspectFlags aspectMask,
            ImageLayout oldImageLayout,
            ImageLayout newImageLayout,
            PipelineStageFlags srcStageMask,
            PipelineStageFlags dstStageMask)
    {
        ImageSubresourceRange subresourceRange = new()
        {
            AspectMask = aspectMask,
            BaseMipLevel = 0,
            LevelCount = 1,
            LayerCount = 1
        };
        SetImageLayout(vk, cmdbuffer, image, oldImageLayout, newImageLayout, subresourceRange, srcStageMask, dstStageMask);
    }

    private static void InsertImageMemoryBarrier(
            Vk vk,
            CommandBuffer cmdbuffer,
            Image image,
            AccessFlags srcAccessMask,
            AccessFlags dstAccessMask,
            ImageLayout oldImageLayout,
            ImageLayout newImageLayout,
            PipelineStageFlags srcStageMask,
            PipelineStageFlags dstStageMask,
            ImageSubresourceRange subresourceRange)
    {
        ImageMemoryBarrier imageMemoryBarrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            SrcAccessMask = srcAccessMask,
            DstAccessMask = dstAccessMask,
            OldLayout = oldImageLayout,
            NewLayout = newImageLayout,
            Image = image,
            SubresourceRange = subresourceRange
        };

        vk.CmdPipelineBarrier(
            cmdbuffer,
            srcStageMask,
            dstStageMask,
            0,
            0, null,
            0, null,
            1, imageMemoryBarrier);
    }


}
