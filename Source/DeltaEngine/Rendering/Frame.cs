using DeltaEngine.ECS;
using DeltaEngine.Files;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace DeltaEngine.Rendering;
internal class Frame : IDisposable
{
    private readonly RenderBase _rendererData;
    private SwapChain _swapChain;

    private Buffer _transformsBuffer;
    private Buffer _renderBuffer;
    private Buffer _parentBuffer;

    private Semaphore imageAvailable;
    private Semaphore renderFinished;
    private Fence queueSubmited;

    private CommandBuffer commandBuffer;

    public void UpdateSwapChain(SwapChain swapChain)
    {
        _swapChain = swapChain;
    }

    public unsafe Frame(RenderBase renderBase, SwapChain swapChain)
    {
        _rendererData = renderBase;
        _swapChain = swapChain;

        FenceCreateInfo fenceInfo = new(StructureType.FenceCreateInfo, null, FenceCreateFlags.SignaledBit);
        SemaphoreCreateInfo semaphoreInfo = new(StructureType.SemaphoreCreateInfo);

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _rendererData.commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        _ = _rendererData.vk.AllocateCommandBuffers(_rendererData.device, allocInfo, out commandBuffer);
        _ = _rendererData.vk.CreateSemaphore(_rendererData.device, semaphoreInfo, null, out imageAvailable);
        _ = _rendererData.vk.CreateSemaphore(_rendererData.device, semaphoreInfo, null, out renderFinished);
        _ = _rendererData.vk.CreateFence(_rendererData.device, fenceInfo, null, out queueSubmited);
    }

    public unsafe void Dispose()
    {
        _rendererData.vk.FreeCommandBuffers(_rendererData.device, _rendererData.commandPool, new Span<CommandBuffer>(ref commandBuffer));
        _rendererData.vk.DestroySemaphore(_rendererData.device, imageAvailable, null);
        _rendererData.vk.DestroySemaphore(_rendererData.device, renderFinished, null);
        _rendererData.vk.DestroyFence(_rendererData.device, queueSubmited, null);
    }

    public unsafe void Draw(RenderPass renderPass, GuidAsset<MaterialData> material, Buffer vbo, Buffer ibo, uint indices, out bool resize)
    {
        resize = false;
    }


    public unsafe void Draw(RenderPass renderPass, Pipeline graphicsPipeline, Buffer vbo, Buffer ibo, uint indices, out bool resize)
    {
        _rendererData.vk.WaitForFences(_rendererData.device, 1, queueSubmited, true, ulong.MaxValue);
        var imageAvailable = stackalloc[] { this.imageAvailable };
        uint imageIndex = 0;
        var res = _swapChain.khrSw.AcquireNextImage(_rendererData.device, _swapChain.swapChain, ulong.MaxValue, *imageAvailable, default, &imageIndex);

        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;

        if (resize)
            return;

        _rendererData.vk.ResetFences(_rendererData.device, 1, queueSubmited);
        _rendererData.vk.ResetCommandBuffer(commandBuffer, 0);

        RecordCommandBuffer(commandBuffer, imageIndex, renderPass, graphicsPipeline, vbo, ibo, indices);

        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };
        var renderFinished = stackalloc[] { this.renderFinished };

        var buffer = commandBuffer;
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = imageAvailable,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = renderFinished,
        };
        _ = _rendererData.vk.QueueSubmit(_rendererData.graphicsQueue, 1, submitInfo, queueSubmited);
        var swapChains = stackalloc[] { _swapChain.swapChain };
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = renderFinished,
            SwapchainCount = 1,
            PSwapchains = swapChains,
            PImageIndices = &imageIndex
        };
        res = _swapChain.khrSw.QueuePresent(_rendererData.presentQueue, presentInfo);
        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
    }

    /// <summary>
    /// Draft drawing method
    /// </summary>
    /// <param name="data"></param>
    internal unsafe void Draw(RenderData[] data)
    {
        var shaderGroups = data.GetGroup(x => x.material.Asset.shader.Asset);
        foreach (var shaderGroup in shaderGroups)
        {
            Console.WriteLine("Binding pipeline bound to shader");
            var materialGroups = shaderGroup.Value.GetGroup(x => x.material);
            foreach (var materialGroup in materialGroups)
            {
                Console.WriteLine("Binding material data to pipeline");

                Console.WriteLine("Grouping meshes with same material");

                var meshGroups = materialGroup.Value.GetGroup(x => x.mesh);

                List<RenderData> staticBatchGroup = new();
                List<RenderData> dynamicBatchGroup = new();

                Console.WriteLine("Instancing draw stage begin");
                foreach (var meshGroup in meshGroups)
                {
                    var instancingGroups = meshGroup.Value.GetGroup(x => x.isStatic);

                    if (instancingGroups.TryGetValue(true, out var staticInstancingObjects))
                    {
                        if (staticInstancingObjects.Count > 1)
                            Console.WriteLine("Static objects with Instancing drawing");
                        else
                            staticBatchGroup.Add(staticInstancingObjects[0]);
                    }
                    if (instancingGroups.TryGetValue(false, out var dynamicInstancingObjects))
                    {
                        if (dynamicInstancingObjects.Count > 1)
                            Console.WriteLine("Dynamic objects with Instancing drawing");
                        else
                            dynamicBatchGroup.Add(dynamicInstancingObjects[0]);
                    }
                }
                Console.WriteLine("Instancing draw stage end");

                Console.WriteLine("Batching draw stage begin");

                Console.WriteLine("Grouping remaining meshes into one _left");

                if (staticBatchGroup.Count > 0)
                    Console.WriteLine("Static objects with Batching drawing");
                if (dynamicBatchGroup.Count > 0)
                    Console.WriteLine("Dynamic objects with Batching drawing");

                Console.WriteLine("Batching draw stage end");
            }
        }
    }

    private unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex, RenderPass renderPass, Pipeline graphicsPipeline, Buffer vbo, Buffer ibo, uint indices)
    {
        CommandBufferBeginInfo beginInfo = new(StructureType.CommandBufferBeginInfo);
        ClearValue clearColor = new(new ClearColorValue(0.05f, 0.05f, 0.05f, 1));
        Rect2D renderRect = new(null, _swapChain.extent);
        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderPass,
            Framebuffer = _swapChain.frameBuffers[(int)imageIndex],
            ClearValueCount = 1,
            PClearValues = &clearColor,
            RenderArea = renderRect
        };
        Viewport viewport = new()
        {
            Width = _swapChain.extent.Width,
            Height = _swapChain.extent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        _ = _rendererData.vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        _rendererData.vk.CmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);
        _rendererData.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, graphicsPipeline);
        _rendererData.vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        _rendererData.vk.CmdSetScissor(commandBuffer, 0, 1, &renderRect);

        var buffer = stackalloc Buffer[] { vbo };
        ulong offsets = 0;

        _rendererData.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, buffer, &offsets);
        _rendererData.vk.CmdBindIndexBuffer(commandBuffer, ibo, 0, IndexType.Uint32);
        _rendererData.vk.CmdDrawIndexed(commandBuffer, indices, 1, 0, 0, 0);
        _rendererData.vk.CmdEndRenderPass(commandBuffer);

        _ = _rendererData.vk.EndCommandBuffer(commandBuffer);
    }
}
