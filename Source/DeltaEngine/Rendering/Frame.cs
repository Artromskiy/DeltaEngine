using Delta.ECS;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Delta.Rendering;
internal class Frame : IDisposable
{
    private readonly RenderBase _rendererData;
    private readonly RenderAssets _renderAssets;
    private SwapChain _swapChain;

    private Semaphore imageAvailable;
    private Semaphore renderFinished;
    private Fence renderFinishedFence;

    private CommandBuffer commandBuffer;
    private DescriptorSet _descriptorSet;

    private readonly DynamicBuffer _matrices;
    private readonly DynamicBuffer _ids;

    private Semaphore _syncSemaphore;

    public void UpdateSwapChain(SwapChain swapChain)
    {
        _swapChain = swapChain;
    }

    public unsafe Frame(RenderBase renderBase, RenderAssets renderAssets, SwapChain swapChain, DescriptorSetLayout descriptorSetLayout)
    {
        _rendererData = renderBase;
        _renderAssets = renderAssets;
        _swapChain = swapChain;
        _matrices = new(renderBase, 1);
        _ids = new(renderBase, 1);

        FenceCreateInfo fenceInfo = new(StructureType.FenceCreateInfo, null, FenceCreateFlags.SignaledBit);
        SemaphoreCreateInfo semaphoreInfo = new(StructureType.SemaphoreCreateInfo);
        _descriptorSet = RenderHelper.CreateDescriptorSet(renderBase, descriptorSetLayout);

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _rendererData.deviceQ.graphicsCmdPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        _ = _rendererData.vk.AllocateCommandBuffers(_rendererData.deviceQ.device, allocInfo, out commandBuffer);
        _ = _rendererData.vk.CreateSemaphore(_rendererData.deviceQ.device, semaphoreInfo, null, out imageAvailable);
        _ = _rendererData.vk.CreateSemaphore(_rendererData.deviceQ.device, semaphoreInfo, null, out renderFinished);
        _ = _rendererData.vk.CreateFence(_rendererData.deviceQ.device, fenceInfo, null, out renderFinishedFence);
    }

    public unsafe void Dispose()
    {
        _rendererData.vk.FreeCommandBuffers(_rendererData.deviceQ.device, _rendererData.deviceQ.graphicsCmdPool, 1, in commandBuffer);
        _rendererData.vk.DestroySemaphore(_rendererData.deviceQ.device, imageAvailable, null);
        _rendererData.vk.DestroySemaphore(_rendererData.deviceQ.device, renderFinished, null);
        _rendererData.vk.DestroyFence(_rendererData.deviceQ.device, renderFinishedFence, null);
    }

    public void Sync()
    {
        _rendererData.vk.WaitForFences(_rendererData.deviceQ.device, 1, renderFinishedFence, true, ulong.MaxValue);
    }

    public bool Synced() => _rendererData.vk.GetFenceStatus(_rendererData.deviceQ.device, renderFinishedFence) == Result.Success;

    public DynamicBuffer GetTRSBuffer() => _matrices;
    public DynamicBuffer GetIdsBuffer() => _ids;

    public void AddSemaphore(Semaphore semaphore)
    {
        _syncSemaphore = semaphore;
    }

    public unsafe void Draw(List<(Render render, uint count)> rendersData, out bool resize)
    {

        //_rendererData.vk.WaitForFences(_rendererData.deviceQueues.device, 1, renderFinishedFence, true, ulong.MaxValue);

        var imageAvailable = this.imageAvailable;
        uint imageIndex = 0;

        var res = _swapChain.khrSw.AcquireNextImage(_rendererData.deviceQ.device, _swapChain.swapChain, ulong.MaxValue, imageAvailable, default, &imageIndex);

        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
        if (resize)
            return;

        _rendererData.vk.ResetFences(_rendererData.deviceQ.device, 1, renderFinishedFence);
        _rendererData.vk.ResetCommandBuffer(commandBuffer, 0);

        if (_matrices.ChangedBuffer)
        {
            RenderHelper.BindBuffersToDescriptorSet(_rendererData, _descriptorSet, _matrices.GetBuffer(), 0, DescriptorType.StorageBuffer);
            _matrices.ChangedBuffer = false;
        }
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
        _ = _rendererData.vk.QueueSubmit(_rendererData.deviceQ.graphicsQueue, 1, submitInfo, renderFinishedFence);
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
        res = _swapChain.khrSw.QueuePresent(_rendererData.deviceQ.presentQueue, presentInfo);
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
            RenderPass = _rendererData.renderPass,
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
        _rendererData.vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        _rendererData.vk.CmdSetScissor(commandBuffer, 0, 1, &renderRect);

        Guid currentShader = Guid.Empty;
        //Guid currentMaterial = Guid.Empty;
        Guid currentMesh = Guid.Empty;
        VertexAttribute attributeMask = (VertexAttribute)(-1);
        uint indicesCount = 0;

        var matrices = _descriptorSet;

        _rendererData.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _rendererData.pipelineLayout, 0, 1, &matrices, 0, 0);
        uint firstInstance = 0;
        foreach (var (rend, count) in renders)
        {
            var itemShader = rend._shader;
            var itemMesh = rend.Mesh;

            if (itemShader != currentShader) // shader switch
            {
                (var pipeline, attributeMask) = _renderAssets.GetPipelineAndAttributes(itemShader);
                _rendererData.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, pipeline);
                currentShader = itemShader;
            }

            // material switch?

            if (itemMesh != currentMesh) // mesh switch
            {
                (var vertices, var indices, indicesCount) = _renderAssets.GetVertexIndexBuffersAndCount(itemMesh, attributeMask);

                _rendererData.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, vertices, 0);
                _rendererData.vk.CmdBindIndexBuffer(commandBuffer, indices, 0, IndexType.Uint32);
            }
            _rendererData.vk.CmdDrawIndexed(commandBuffer, indicesCount, count, 0, 0, firstInstance);
            firstInstance += count;
        }

        _rendererData.vk.CmdEndRenderPass(commandBuffer);

        _ = _rendererData.vk.EndCommandBuffer(commandBuffer);
    }
}