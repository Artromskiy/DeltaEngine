using Delta.Files;
using Delta.Rendering.Internal;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.SDL;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Delta.Rendering;

internal static class RenderHelper
{
    private const uint flags = (uint)
    (
          WindowFlags.Vulkan
        | WindowFlags.Shown
        | WindowFlags.Resizable
        | WindowFlags.AllowHighdpi
        | WindowFlags.Borderless
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
            SType = StructureType.InstanceCreateInfo,
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

        FenceCreateInfo fenceInfo = new(StructureType.FenceCreateInfo, null, FenceCreateFlags.SignaledBit);

        for (var i = 0; i < swapChainImagesCount; i++)
        {
            _ = api.vk.CreateSemaphore(device, semaphoreInfo, null, out imageAvailableSemaphores[i]);
            _ = api.vk.CreateSemaphore(device, semaphoreInfo, null, out renderFinishedSemaphores[i]);
            _ = api.vk.CreateFence(device, fenceInfo, null, out inFlightFences[i]);
        }
        return (imageAvailableSemaphores, renderFinishedSemaphores, inFlightFences);
    }

    public static unsafe Fence CreateFence(RenderBase data, bool signaled)
    {
        FenceCreateFlags flag = signaled ? FenceCreateFlags.SignaledBit : FenceCreateFlags.None;
        FenceCreateInfo fenceCreate = new(StructureType.FenceCreateInfo, null, flag);
        _ = data.vk.CreateFence(data.deviceQ.device, fenceCreate, null, out var result);
        return result;
    }

    public static unsafe Semaphore CreateSemaphore(RenderBase data)
    {
        SemaphoreCreateInfo semaphoreCreate = new(StructureType.SemaphoreCreateInfo);
        _ = data.vk.CreateSemaphore(data.deviceQ.device, semaphoreCreate, null, out var result);
        return result;
    }

    internal static unsafe void CopyBuffer<T>(this RenderBase data, GpuArray<T> source, DynamicBuffer destination,
        CommandBuffer cmdBuffer) where T : unmanaged
    {
        destination.EnsureSize(source.Size);
        data.CopyBuffer(source.GetBuffer(), source.Size, destination.GetBuffer(), destination.Size, cmdBuffer);
    }

    internal static unsafe void CopyBuffer<T>(this RenderBase data, GpuArray<T> source, DynamicBuffer destination,
        Fence fence, Semaphore semaphore, CommandBuffer cmdBuffer) where T : unmanaged
    {
        destination.EnsureSize(source.Size);
        data.CopyBuffer(source.GetBuffer(), source.Size, destination.GetBuffer(), destination.Size, fence, semaphore, cmdBuffer);
    }
    internal static unsafe void CopyBuffer<T>(this RenderBase data, GpuArray<T> source, DynamicBuffer destination,
        Fence fence, CommandBuffer cmdBuffer) where T : unmanaged
    {
        destination.EnsureSize(source.Size);
        data.CopyBuffer(source.GetBuffer(), source.Size, destination.GetBuffer(), destination.Size, fence, cmdBuffer);
    }


    public static unsafe void BeginCmdBuffer(this RenderBase data, CommandBuffer cmdBuffer)
    {
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _ = data.vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
    }

    internal static unsafe void CopyCmd<T>(this RenderBase data, GpuArray<T> source, DynamicBuffer destination, CommandBuffer cmdBuffer) where T : unmanaged
    {
        destination.EnsureSize(source.Size);
        BufferCopy copy = new(0, 0, Math.Min(source.Size, destination.Size));
        data.vk.CmdCopyBuffer(cmdBuffer, source.GetBuffer(), destination.GetBuffer(), 1, &copy);
    }

    public static unsafe void EndCmdBuffer(this RenderBase data, Queue queue, CommandBuffer cmdBuffer, Fence fence, Semaphore semaphore)
    {
        _ = data.vk.EndCommandBuffer(cmdBuffer);
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &semaphore,
        };
        _ = data.vk.QueueSubmit(queue, 1, &submitInfo, fence);
    }

    public static unsafe void CopyBuffer(this RenderBase data, Buffer source, ulong sourceSize, Buffer destination, ulong destinationSize, Fence fence, Semaphore semaphore, CommandBuffer cmdBuffer)
    {
        Vk vk = data.vk;
        vk.ResetFences(data.deviceQ.device, 1, in fence);
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _ = vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
        BufferCopy copy = new(0, 0, Math.Min(sourceSize, destinationSize));
        vk.CmdCopyBuffer(cmdBuffer, source, destination, 1, &copy);
        _ = vk.EndCommandBuffer(cmdBuffer);
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &semaphore,
        };
        _ = vk.QueueSubmit(data.deviceQ.transferQueue, 1, &submitInfo, fence);
    }


    public static unsafe void CopyBuffer(this RenderBase data, Buffer source, ulong sourceSize, Buffer destination, ulong destinationSize, Fence fence, CommandBuffer cmdBuffer)
    {
        Vk vk = data.vk;
        vk.ResetFences(data.deviceQ.device, 1, in fence);
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _ = vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
        BufferCopy copy = new(0, 0, Math.Min(sourceSize, destinationSize));
        vk.CmdCopyBuffer(cmdBuffer, source, destination, 1, &copy);
        _ = vk.EndCommandBuffer(cmdBuffer);
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
        };
        _ = vk.QueueSubmit(data.deviceQ.transferQueue, 1, &submitInfo, fence);
    }

    public static unsafe void CopyBuffer(this RenderBase data, Buffer source, ulong sourceSize, Buffer destination, ulong destinationSize, CommandBuffer cmdBuffer)
    {
        Vk vk = data.vk;
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _ = vk.BeginCommandBuffer(cmdBuffer, &beginInfo);
        BufferCopy copy = new(0, 0, Math.Min(sourceSize, destinationSize));
        vk.CmdCopyBuffer(cmdBuffer, source, destination, 1, &copy);
        _ = vk.EndCommandBuffer(cmdBuffer);
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer,
        };
        _ = vk.QueueSubmit(data.deviceQ.transferQueue, 1, &submitInfo, default);
        vk.DeviceWaitIdle(data.deviceQ.device);
    }

    public static unsafe RenderPass CreateRenderPass(Vk vk, Device device, Format swapChainImageFormat)
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

    public static unsafe string[] GetRequiredVulkanExtensions(Sdl sdl, Window* window)
    {
        uint extCount = 0;
        _ = sdl.VulkanGetInstanceExtensions(window, &extCount, (byte**)null);
        string[] extensions = new string[extCount];
        _ = sdl.VulkanGetInstanceExtensions(window, &extCount, extensions);
        return extensions;
    }

    public static unsafe string[] GetVulkanLayers(Api api, string[] reqVkLayers)
    {
        uint layersCount = 0;
        _ = api.vk.EnumerateInstanceLayerProperties(&layersCount, null);
        Span<LayerProperties> layers = stackalloc LayerProperties[(int)layersCount];
        _ = api.vk.EnumerateInstanceLayerProperties(&layersCount, layers);
        HashSet<string> layersNames = [];
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

    public static unsafe (Buffer buffer, DeviceMemory memory) CreateBufferAndMemory(RenderBase data, ulong size, BufferUsageFlags bufferUsageFlags, MemoryPropertyFlags memoryPropertyFlags)
    {
        BufferCreateInfo createInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = bufferUsageFlags,
            SharingMode = SharingMode.Exclusive,
            Flags = default,
        };
        _ = data.vk.CreateBuffer(data.deviceQ.device, createInfo, null, out var buffer);
        var reqs = data.vk.GetBufferMemoryRequirements(data.deviceQ.device, buffer);
        MemoryAllocateInfo allocateInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = reqs.Size,
            MemoryTypeIndex = FindMemoryType(data, (int)reqs.MemoryTypeBits, memoryPropertyFlags)
        };
        _ = data.vk.AllocateMemory(data.deviceQ.device, allocateInfo, null, out var memory);
        _ = data.vk.BindBufferMemory(data.deviceQ.device, buffer, memory, 0);
        return (buffer, memory);
    }

    public static unsafe (Buffer buffer, DeviceMemory memory) CreateVertexBuffer(RenderBase data, MeshData meshData, VertexAttribute attributeMask)
    {
        var size = (uint)(attributeMask.GetVertexSize() * meshData.vertexCount);
        var res = CreateBufferAndMemory(data, size, BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        void* datap;
        data.vk.MapMemory(data.deviceQ.device, res.memory, 0, size, 0, &datap);

        Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(datap),
            ref MemoryMarshal.GetArrayDataReference(MeshCollection.GetMeshVariant(meshData, attributeMask)),
            size);

        data.vk.UnmapMemory(data.deviceQ.device, res.memory);
        return res;
    }
    public static unsafe (Buffer buffer, DeviceMemory memory) CreateIndexBuffer(RenderBase data, MeshData meshData)
    {
        var size = (uint)(sizeof(uint) * meshData.GetIndices().Length);
        var res = CreateBufferAndMemory(data, size, BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        void* datap;
        data.vk.MapMemory(data.deviceQ.device, res.memory, 0, size, 0, &datap);

        Span<uint> dataSpan = new(datap, meshData.GetIndices().Length);
        meshData.GetIndices().CopyTo(dataSpan);

        data.vk.UnmapMemory(data.deviceQ.device, res.memory);
        return res;
    }


    public static unsafe (Buffer, DeviceMemory) CreateIndexBuffer(RenderBase data, uint[] indices)
    {
        uint size = (uint)(sizeof(uint) * indices.Length);
        var res = CreateBufferAndMemory(data, size, BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        void* datap;
        data.vk.MapMemory(data.deviceQ.device, res.memory, 0, size, 0, &datap);

        Unsafe.CopyBlockUnaligned(ref Unsafe.AsRef<byte>(datap),
            ref Unsafe.As<uint, byte>(ref MemoryMarshal.GetArrayDataReference(indices)),
            size);

        data.vk.UnmapMemory(data.deviceQ.device, res.memory);
        return res;
    }

    public static uint FindMemoryType(RenderBase data, int typeFilter, MemoryPropertyFlags properties)
    {
        int i = 0;
        for (; i < data.memoryProperties.MemoryTypeCount; i++)
            if (Convert.ToBoolean(typeFilter & (1 << i)) && (data.memoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties) // some mask magic
                return (uint)i;
        _ = false;
        return (uint)i;
    }

    public static uint FindMemoryType(RenderBase data, int typeFilter, MemoryPropertyFlags properties, out MemoryPropertyFlags memoryFlagsHas)
    {
        memoryFlagsHas = MemoryPropertyFlags.None;
        int i = 0;
        for (; i < data.memoryProperties.MemoryTypeCount; i++)
            if (Convert.ToBoolean(typeFilter & (1 << i)) && (data.memoryProperties.MemoryTypes[i].PropertyFlags & properties) == properties) // some mask magic
            {
                memoryFlagsHas = data.memoryProperties.MemoryTypes[i].PropertyFlags;
                return (uint)i;
            }
        _ = false;
        return (uint)i;
    }

    public static VertexInputBindingDescription GetBindingDescription(VertexAttribute vertexAttributeMask) => new()
    {
        Binding = 0,
        InputRate = VertexInputRate.Vertex,
        Stride = (uint)vertexAttributeMask.GetVertexSize()
    };

    public static unsafe void FillAttributeDesctiption(Span<VertexInputAttributeDescription> description, VertexAttribute vertexAttributeMask)
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

    public static unsafe VertexAttribute GetInputAttributes(ReadOnlySpan<byte> shaderCode)
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

    public static unsafe Pipeline CreateGraphicsPipeline(ShaderData shaderData, RenderBase data, out VertexAttribute attributeMask)
    {
        using var vertShader = new PipelineShader(data, ShaderStageFlags.VertexBit, shaderData.GetVertBytes());
        using var fragShader = new PipelineShader(data, ShaderStageFlags.FragmentBit, shaderData.GetFragBytes());
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
            Layout = data.pipelineLayout,
            RenderPass = data.renderPass,
            Subpass = 0,
            BasePipelineHandle = default,
        };
        _ = data.vk.CreateGraphicsPipelines(data.deviceQ.device, default, 1, pipelineInfo, null, out var graphicsPipeline);
        return graphicsPipeline;
    }

    public static unsafe Pipeline CreateGraphicsPipeline(RenderBase data)
    {
        var vertPath = "shaders/vert.spv";
        var fragPath = "shaders/frag.spv";
        using var vertShader = new PipelineShader(data, ShaderStageFlags.VertexBit, vertPath);
        using var fragShader = new PipelineShader(data, ShaderStageFlags.FragmentBit, fragPath);
        var stages = stackalloc PipelineShaderStageCreateInfo[2]
        {
            ShaderModuleGroupCreator.Create(vertShader),
            ShaderModuleGroupCreator.Create(fragShader)
        };
        var attributeMask = GetInputAttributes(File.ReadAllBytes(vertPath));
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
            Layout = data.pipelineLayout,
            RenderPass = data.renderPass,
            Subpass = 0,
            BasePipelineHandle = default,
        };
        _ = data.vk.CreateGraphicsPipelines(data.deviceQ.device, default, 1, pipelineInfo, null, out var graphicsPipeline);
        return graphicsPipeline;
    }


    public static unsafe PipelineLayout CreatePipelineLayout(Vk vk, Device device, ReadOnlySpan<DescriptorSetLayout> layouts)
    {
        PipelineLayout result;
        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            PipelineLayoutCreateInfo pipelineLayoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint)layouts.Length,
                PSetLayouts = layoutsPtr,
            };
            _ = vk.CreatePipelineLayout(device, pipelineLayoutInfo, null, out result);
        }
        return result;
    }

    public static unsafe ImmutableArray<Framebuffer> CreateFramebuffers(Api api, Device device, ReadOnlySpan<ImageView> swapChainImageViews, RenderPass renderPass, Extent2D swapChainExtent)
    {
        Span<Framebuffer> swapChainFramebuffers = stackalloc Framebuffer[swapChainImageViews.Length];
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
            _ = api.vk.CreateFramebuffer(device, framebufferInfo, null, out swapChainFramebuffers[i]);
        }
        return ImmutableArray.Create(swapChainFramebuffers);
    }


    public static unsafe DescriptorPool CreateDescriptorPool(RenderBase data)
    {
        uint descriptorCount = 10;
        DescriptorPoolSize poolSize = new()
        {
            DescriptorCount = descriptorCount,
            Type = DescriptorType.StorageBuffer
        };
        DescriptorPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
            MaxSets = descriptorCount,
        };
        _ = data.vk.CreateDescriptorPool(data.deviceQ.device, poolInfo, null, out var result);
        return result;
    }

    public static unsafe DescriptorSet CreateDescriptorSet(RenderBase data, DescriptorSetLayout descriptorSetLayout)
    {
        DescriptorSetAllocateInfo allocateInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = data.descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &descriptorSetLayout,
        };
        _ = data.vk.AllocateDescriptorSets(data.deviceQ.device, allocateInfo, out var result);
        return result;
    }

    public static unsafe void BindBuffersToDescriptorSet(RenderBase data, DescriptorSet descriptorSet, Buffer buffer, uint binding, DescriptorType bufferUsage)
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
        data.vk.UpdateDescriptorSets(data.deviceQ.device, 1, descriptorWrite, 0, null);
    }

    internal static unsafe CommandBuffer CreateCommandBuffer(this RenderBase data, CommandPool cmdPool)
    {
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = cmdPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        CommandBuffer commandBuffer;
        _ = data.vk.AllocateCommandBuffers(data.deviceQ.device, allocInfo, &commandBuffer);
        return commandBuffer;
    }


    public static unsafe PhysicalDevice PickPhysicalDevice(Vk vk, Instance instance, SurfaceKHR surface, KhrSurface khrsf, string[] neededExtensions)
    {
        uint deviceCount = 0;
        _ = vk.EnumeratePhysicalDevices(instance, &deviceCount, null);
        Span<PhysicalDevice> devices = stackalloc PhysicalDevice[(int)deviceCount];
        PhysicalDevice selected = default;
        bool discrete = false;
        bool suitable = false;
        vk.EnumeratePhysicalDevices(instance, &deviceCount, devices);
        foreach (var device in devices)
        {
            vk.GetPhysicalDeviceProperties(device, out var props);
            suitable = IsDeviceSuitable(vk, device, surface, khrsf, neededExtensions);
            if (suitable)
                selected = device;
            discrete = props.DeviceType == PhysicalDeviceType.DiscreteGpu;
            if (discrete && suitable)
                return device;
        }
        return selected;
    }

    public static unsafe ImmutableArray<ImageView> CreateImageViews(Api api, Device device, ReadOnlySpan<Image> swapChainImages, Format imageFormat)
    {
        Span<ImageView> swapChainImageViews = stackalloc ImageView[swapChainImages.Length];
        for (int i = 0; i < swapChainImages.Length; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = swapChainImages[i],
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
            _ = api.vk.CreateImageView(device, createInfo, null, out swapChainImageViews[i]);
        }
        return ImmutableArray.Create(swapChainImageViews);
    }


    internal static unsafe DeviceQueues CreateLogicalDevice(Vk vk, PhysicalDevice gpu, SurfaceKHR surface, KhrSurface khrsf, string[] deviceExtensions)
    {
        var indices = new QueueFamilyIndiciesDetails(vk, surface, gpu, khrsf);
        return new DeviceQueues(vk, gpu, indices, deviceExtensions);
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
