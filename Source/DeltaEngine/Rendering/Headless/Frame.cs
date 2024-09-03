using Delta.ECS;
using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace Delta.Rendering.Headless;
internal class Frame : IDisposable
{
    private readonly RenderBase _rendererBase;
    private readonly RenderAssets _renderAssets;
    private SwapChain _swapChain;

    private Semaphore _presentSemaphore;
    private Semaphore _renderedSemaphore;
    private Fence _renderFinishedFence;

    private CommandBuffer _commandBuffer;

    private readonly Dictionary<IRenderBatcher, DescriptorSets> _batchedSets = [];

    private Semaphore _batchersSemaphore;

    public void UpdateSwapChain(SwapChain swapChain)
    {
        _swapChain = swapChain;
    }

    public unsafe Frame(RenderBase renderBase, RenderAssets renderAssets, SwapChain swapChain)
    {
        _rendererBase = renderBase;
        _renderAssets = renderAssets;
        _swapChain = swapChain;

        FenceCreateInfo fenceInfo = new(StructureType.FenceCreateInfo, null, FenceCreateFlags.SignaledBit);
        SemaphoreCreateInfo semaphoreInfo = new(StructureType.SemaphoreCreateInfo);

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _rendererBase.deviceQ.GetCmdPool(QueueType.Graphics),
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        _ = _rendererBase.vk.AllocateCommandBuffers(_rendererBase.deviceQ, allocInfo, out _commandBuffer);
        _ = _rendererBase.vk.CreateSemaphore(_rendererBase.deviceQ, semaphoreInfo, null, out _presentSemaphore);
        _ = _rendererBase.vk.CreateSemaphore(_rendererBase.deviceQ, semaphoreInfo, null, out _renderedSemaphore);
        _ = _rendererBase.vk.CreateFence(_rendererBase.deviceQ, fenceInfo, null, out _renderFinishedFence);
    }

    public unsafe void Dispose()
    {
        _rendererBase.vk.FreeCommandBuffers(_rendererBase.deviceQ,
            _rendererBase.deviceQ.GetCmdPool(QueueType.Graphics), 1, in _commandBuffer);

        _rendererBase.vk.DestroySemaphore(_rendererBase.deviceQ, _presentSemaphore, null);
        _rendererBase.vk.DestroySemaphore(_rendererBase.deviceQ, _renderedSemaphore, null);
        _rendererBase.vk.DestroyFence(_rendererBase.deviceQ, _renderFinishedFence, null);

        foreach (var item in _batchedSets)
            item.Value.Dispose();
        _batchedSets.Clear();
    }
    public void CopyBatchersData(CommandBuffer commandBuffer)
    {
        foreach (var item in _batchedSets)
            item.Value.CopyBatcherData(item.Key, commandBuffer);
    }
    public void Sync() => _rendererBase.vk.WaitForFences(_rendererBase.deviceQ, 1, _renderFinishedFence, true, ulong.MaxValue);
    public bool Synced() => _rendererBase.vk.GetFenceStatus(_rendererBase.deviceQ, _renderFinishedFence) == Result.Success;
    public void AddSemaphore(Semaphore semaphore) => _batchersSemaphore = semaphore;
    public void AddBatcher(IRenderBatcher renderBatcher)
    {
        var sets = new DescriptorSets(_rendererBase.vk, _rendererBase.deviceQ,
            _rendererBase.descriptorPool, _rendererBase.pipelineLayout, _rendererBase.descriptorSetLayouts);
        _batchedSets.Add(renderBatcher, sets);
    }
    public void RemoveBatcher(IRenderBatcher renderBatcher)
    {
        if (_batchedSets.Remove(renderBatcher, out var sets))
            sets.Dispose();
    }

    public unsafe void Draw()
    {
        //_rendererData.vk.WaitForFences(_rendererData.deviceQueues.device, 1, renderFinishedFence, true, ulong.MaxValue);
        int imageIndex = 0;

        imageIndex = _swapChain.AcquireNextImage();

        _rendererBase.vk.ResetFences(_rendererBase.deviceQ, 1, _renderFinishedFence);
        _rendererBase.vk.ResetCommandBuffer(_commandBuffer, 0);

        foreach (var item in _batchedSets)
            item.Value.UpdateDescriptorSets();
        BeginRecordCommandBuffer(imageIndex);
        foreach (var item in _batchedSets)
            RecordCommandBuffer(item.Value);
        EndRecordCommandBuffer();

        var buffer = _commandBuffer;
        var batchersSemaphore = _batchersSemaphore;
        var renderedSemaphore = _renderedSemaphore;

        var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.ColorAttachmentOutputBit };

        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            PWaitDstStageMask = waitStages,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &batchersSemaphore,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &renderedSemaphore,
        };
        _ = _rendererBase.vk.QueueSubmit(_rendererBase.deviceQ.GetQueue(QueueType.Graphics), 1, submitInfo, _renderFinishedFence);
        _swapChain.Present(renderedSemaphore);
    }

    private unsafe void BeginRecordCommandBuffer(int imageIndex)
    {
        CommandBufferBeginInfo beginInfo = new(StructureType.CommandBufferBeginInfo);
        ClearValue clearColor = new(new ClearColorValue(0.05f, 0.05f, 0.05f, 1));
        Rect2D renderRect = new(null, _swapChain.Extent);
        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _rendererBase.renderPass,
            Framebuffer = _swapChain.frameBuffers[imageIndex],
            ClearValueCount = 1,
            PClearValues = &clearColor,
            RenderArea = renderRect
        };
        Viewport viewport = new()
        {
            Width = _swapChain.Extent.Width,
            Height = _swapChain.Extent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        _ = _rendererBase.vk.BeginCommandBuffer(_commandBuffer, &beginInfo);

        _rendererBase.vk.CmdBeginRenderPass(_commandBuffer, &renderPassInfo, SubpassContents.Inline);
        _rendererBase.vk.CmdSetViewport(_commandBuffer, 0, 1, &viewport);
        _rendererBase.vk.CmdSetScissor(_commandBuffer, 0, 1, &renderRect);
    }

    private unsafe void RecordCommandBuffer(DescriptorSets frameDescriptorSets)
    {
        Guid currentShader = Guid.Empty;
        //Guid currentMaterial = Guid.Empty;
        Guid currentMesh = Guid.Empty;
        VertexAttribute attributeMask = (VertexAttribute)(-1);
        uint indicesCount = 0;

        frameDescriptorSets.BindDescriptorSets(_commandBuffer);

        uint firstInstance = 0;
        foreach (var (rend, count) in frameDescriptorSets.RenderList)
        {
            var itemShader = rend._shader;
            var itemMesh = rend.mesh;

            if (itemShader.guid != currentShader) // shader switch
            {
                (var pipeline, attributeMask) = _renderAssets.GetPipelineAndAttributes(itemShader);
                _rendererBase.vk.CmdBindPipeline(_commandBuffer, PipelineBindPoint.Graphics, pipeline);
                currentShader = itemShader.guid;
            }

            // material switch?

            if (itemMesh.guid != currentMesh) // mesh switch
            {
                (var vertices, var indices, indicesCount) = _renderAssets.GetVertexIndexBuffersAndCount(itemMesh, attributeMask);

                _rendererBase.vk.CmdBindVertexBuffers(_commandBuffer, 0, 1, vertices, 0);
                _rendererBase.vk.CmdBindIndexBuffer(_commandBuffer, indices, 0, IndexType.Uint32);
            }
            _rendererBase.vk.CmdDrawIndexed(_commandBuffer, indicesCount, count, 0, 0, firstInstance);
            firstInstance += count;
        }

    }

    private void EndRecordCommandBuffer()
    {
        _rendererBase.vk.CmdEndRenderPass(_commandBuffer);
        _ = _rendererBase.vk.EndCommandBuffer(_commandBuffer);
    }
}