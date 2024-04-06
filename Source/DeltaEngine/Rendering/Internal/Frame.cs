using Delta.ECS.Components;
using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace Delta.Rendering;
internal class Frame : IDisposable
{
    private readonly RenderBase _rendererBase;
    private readonly RenderAssets _renderAssets;
    private SwapChain _swapChain;

    private Semaphore imageAvailable;
    private Semaphore renderFinished;
    private Fence renderFinishedFence;

    private CommandBuffer commandBuffer;


    private readonly FrameDescriptorSets _descriptorSets;


    private Semaphore _syncSemaphore;

    public void UpdateSwapChain(SwapChain swapChain)
    {
        _swapChain = swapChain;
    }

    public unsafe Frame(RenderBase renderBase, RenderAssets renderAssets, SwapChain swapChain)
    {
        _rendererBase = renderBase;
        _renderAssets = renderAssets;
        _swapChain = swapChain;

        _descriptorSets = new FrameDescriptorSets(renderBase);

        FenceCreateInfo fenceInfo = new(StructureType.FenceCreateInfo, null, FenceCreateFlags.SignaledBit);
        SemaphoreCreateInfo semaphoreInfo = new(StructureType.SemaphoreCreateInfo);


        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _rendererBase.deviceQ.graphicsCmdPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        _ = _rendererBase.vk.AllocateCommandBuffers(_rendererBase.deviceQ, allocInfo, out commandBuffer);
        _ = _rendererBase.vk.CreateSemaphore(_rendererBase.deviceQ, semaphoreInfo, null, out imageAvailable);
        _ = _rendererBase.vk.CreateSemaphore(_rendererBase.deviceQ, semaphoreInfo, null, out renderFinished);
        _ = _rendererBase.vk.CreateFence(_rendererBase.deviceQ, fenceInfo, null, out renderFinishedFence);
    }

    public unsafe void Dispose()
    {
        _rendererBase.vk.FreeCommandBuffers(_rendererBase.deviceQ, _rendererBase.deviceQ.graphicsCmdPool, 1, in commandBuffer);
        _rendererBase.vk.DestroySemaphore(_rendererBase.deviceQ, imageAvailable, null);
        _rendererBase.vk.DestroySemaphore(_rendererBase.deviceQ, renderFinished, null);
        _rendererBase.vk.DestroyFence(_rendererBase.deviceQ, renderFinishedFence, null);

        _descriptorSets.Dispose();
    }

    public void Sync()
    {
        _rendererBase.vk.WaitForFences(_rendererBase.deviceQ, 1, renderFinishedFence, true, ulong.MaxValue);
    }

    public bool Synced() => _rendererBase.vk.GetFenceStatus(_rendererBase.deviceQ, renderFinishedFence) == Result.Success;

    public DynamicBuffer GetTRSBuffer() => _descriptorSets.Matrices;
    public DynamicBuffer GetIdsBuffer() => _descriptorSets.Ids;

    public void AddSemaphore(Semaphore semaphore)
    {
        _syncSemaphore = semaphore;
    }

    public unsafe void Draw(List<(Render render, uint count)> rendersData, out bool resize)
    {
        //_rendererData.vk.WaitForFences(_rendererData.deviceQueues.device, 1, renderFinishedFence, true, ulong.MaxValue);

        var imageAvailable = this.imageAvailable;
        uint imageIndex = 0;

        var res = _swapChain.khrSw.AcquireNextImage(_rendererBase.deviceQ, _swapChain.swapChain, ulong.MaxValue, imageAvailable, default, &imageIndex);

        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
        if (resize)
            return;

        _rendererBase.vk.ResetFences(_rendererBase.deviceQ, 1, renderFinishedFence);
        _rendererBase.vk.ResetCommandBuffer(commandBuffer, 0);

        _descriptorSets.UpdateDescriptorSets();

        // if (_matrices.ChangedBuffer)
        // {
        //     RenderHelper.BindBuffersToDescriptorSet(_rendererBase, _instanceDescriptorSet, _matrices.GetBuffer(), 0, DescriptorType.StorageBuffer);
        //     _matrices.ChangedBuffer = false;
        // }
        // if (_ids.ChangedBuffer)
        // {
        //     RenderHelper.BindBuffersToDescriptorSet(_rendererBase, _instanceDescriptorSet, _ids.GetBuffer(), 1, DescriptorType.StorageBuffer);
        //     _matrices.ChangedBuffer = false;
        // }

        RecordCommandBuffer(rendersData, commandBuffer, imageIndex);

        var buffer = commandBuffer;
        var syncSemaphore = _syncSemaphore;

        var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.ColorAttachmentOutputBit };
        var waitBeforeRender = stackalloc Semaphore[2] { imageAvailable, syncSemaphore };
        var renderFinished = this.renderFinished;

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 2,
            PWaitSemaphores = waitBeforeRender,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &renderFinished,
        };
        _ = _rendererBase.vk.QueueSubmit(_rendererBase.deviceQ.graphicsQueue, 1, submitInfo, renderFinishedFence);
        var swapChain = _swapChain.swapChain;
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &renderFinished,
            SwapchainCount = 1,
            PSwapchains = &swapChain,
            PImageIndices = &imageIndex
        };
        res = _swapChain.khrSw.QueuePresent(_rendererBase.deviceQ.presentQueue, presentInfo);
        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
        if (!resize)
            _ = res;
    }

    private unsafe void RecordCommandBuffer(List<(Render rend, uint count)> renders, CommandBuffer commandBuffer, uint imageIndex)
    {
        CommandBufferBeginInfo beginInfo = new(StructureType.CommandBufferBeginInfo);
        ClearValue clearColor = new(new ClearColorValue(0.05f, 0.05f, 0.05f, 1));
        Rect2D renderRect = new(null, _swapChain.extent);
        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _rendererBase.renderPass,
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
        _ = _rendererBase.vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        _rendererBase.vk.CmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);
        _rendererBase.vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        _rendererBase.vk.CmdSetScissor(commandBuffer, 0, 1, &renderRect);

        Guid currentShader = Guid.Empty;
        //Guid currentMaterial = Guid.Empty;
        Guid currentMesh = Guid.Empty;
        VertexAttribute attributeMask = (VertexAttribute)(-1);
        uint indicesCount = 0;

        //var matrices = _instanceDescriptorSet;

        _descriptorSets.BindDescriptorSets(commandBuffer);

        //_rendererBase.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _rendererBase.pipelineLayout, 0, 1, &matrices, 0, 0);
        uint firstInstance = 0;
        foreach (var (rend, count) in renders)
        {
            var itemShader = rend._shader;
            var itemMesh = rend.Mesh;

            if (itemShader.guid != currentShader) // shader switch
            {
                (var pipeline, attributeMask) = _renderAssets.GetPipelineAndAttributes(itemShader);
                _rendererBase.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);
                currentShader = itemShader.guid;
            }

            // material switch?

            if (itemMesh.guid != currentMesh) // mesh switch
            {
                (var vertices, var indices, indicesCount) = _renderAssets.GetVertexIndexBuffersAndCount(itemMesh, attributeMask);

                _rendererBase.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, vertices, 0);
                _rendererBase.vk.CmdBindIndexBuffer(commandBuffer, indices, 0, IndexType.Uint32);
            }
            _rendererBase.vk.CmdDrawIndexed(commandBuffer, indicesCount, count, 0, 0, firstInstance);
            firstInstance += count;
        }

        _rendererBase.vk.CmdEndRenderPass(commandBuffer);

        _ = _rendererBase.vk.EndCommandBuffer(commandBuffer);
    }
}