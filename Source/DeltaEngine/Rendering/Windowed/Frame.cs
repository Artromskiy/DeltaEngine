using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace Delta.Rendering.Windowed;
internal class Frame : IDisposable
{
    private readonly RenderBase _rendererBase;
    private readonly RenderAssets _renderAssets;
    private SwapChain _swapChain;

    private Semaphore _presentSemaphore;
    private Semaphore _renderedSemaphore;
    private Fence _renderFinishedFence;

    private CommandBuffer _graphCmdBuf;
    private CommandBuffer _comptCmdBuf;

    private readonly Dictionary<IRenderBatcher, DynamicDescriptorSets> _batchedSets = [];

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

        CommandBufferAllocateInfo graphAllocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _rendererBase.deviceQ.GetCmdPool(QueueType.Graphics),
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        CommandBufferAllocateInfo comptAllocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _rendererBase.deviceQ.GetCmdPool(QueueType.Compute),
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        _ = _rendererBase.vk.AllocateCommandBuffers(_rendererBase.deviceQ, graphAllocInfo, out _graphCmdBuf);
        _ = _rendererBase.vk.AllocateCommandBuffers(_rendererBase.deviceQ, comptAllocInfo, out _comptCmdBuf);
        _ = _rendererBase.vk.CreateSemaphore(_rendererBase.deviceQ, semaphoreInfo, null, out _presentSemaphore);
        _ = _rendererBase.vk.CreateSemaphore(_rendererBase.deviceQ, semaphoreInfo, null, out _renderedSemaphore);
        _ = _rendererBase.vk.CreateFence(_rendererBase.deviceQ, fenceInfo, null, out _renderFinishedFence);
    }

    public unsafe void Dispose()
    {
        _rendererBase.vk.FreeCommandBuffers(_rendererBase.deviceQ,
            _rendererBase.deviceQ.GetCmdPool(QueueType.Graphics), 1, in _graphCmdBuf);
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
            item.Value.CopyBatcherData(commandBuffer);
    }
    public void Sync() => _rendererBase.vk.WaitForFences(_rendererBase.deviceQ, 1, _renderFinishedFence, true, ulong.MaxValue);
    public bool Synced() => _rendererBase.vk.GetFenceStatus(_rendererBase.deviceQ, _renderFinishedFence) == Result.Success;
    public void AddSemaphore(Semaphore semaphore) => _batchersSemaphore = semaphore;
    public void AddBatcher(IRenderBatcher renderBatcher)
    {
        var sets = new DynamicDescriptorSets(_rendererBase.vk, _rendererBase.deviceQ,
            _rendererBase.descriptorPool, renderBatcher);
        _batchedSets.Add(renderBatcher, sets);
    }
    public void RemoveBatcher(IRenderBatcher renderBatcher)
    {
        if (_batchedSets.Remove(renderBatcher, out var sets))
            sets.Dispose();
    }

    public unsafe void Draw(out bool resize)
    {
        //_rendererData.vk.WaitForFences(_rendererData.deviceQueues.device, 1, renderFinishedFence, true, ulong.MaxValue);

        var imageIndex = _swapChain.GetImageIndex(_presentSemaphore, out resize);
        if (resize)
            return;

        _rendererBase.vk.ResetFences(_rendererBase.deviceQ, 1, _renderFinishedFence);
        _rendererBase.vk.ResetCommandBuffer(_graphCmdBuf, 0);

        foreach (var item in _batchedSets)
            item.Value.UpdateDescriptorSets();

        // Compute stage

        BeginRecordCommandBuffer(checked((int)imageIndex));
        foreach (var item in _batchedSets)
            RecordCommandBuffer(item.Key, item.Value);
        EndRecordCommandBuffer();

        var buffer = _graphCmdBuf;
        var batchersSemaphore = _batchersSemaphore;

        var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.ColorAttachmentOutputBit };
        var waitBeforeRender = stackalloc Semaphore[2] { _presentSemaphore, batchersSemaphore };
        var renderFinished = _renderedSemaphore;

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
        _ = _rendererBase.vk.QueueSubmit(_rendererBase.deviceQ.GetQueue(QueueType.Graphics), 1, submitInfo, _renderFinishedFence);
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
        var res = _swapChain.khrSw.QueuePresent(_rendererBase.deviceQ.GetQueue(QueueType.Present), presentInfo);
        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
        if (!resize)
            _ = res;
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
            Height = -_swapChain.Extent.Height,
            Y = _swapChain.Extent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };
        _ = _rendererBase.vk.BeginCommandBuffer(_graphCmdBuf, &beginInfo);

        _rendererBase.vk.CmdBeginRenderPass(_graphCmdBuf, &renderPassInfo, SubpassContents.Inline);
        _rendererBase.vk.CmdSetViewport(_graphCmdBuf, 0, 1, &viewport);
        _rendererBase.vk.CmdSetScissor(_graphCmdBuf, 0, 1, &renderRect);
    }

    private unsafe void RecordCommandBuffer(IRenderBatcher batcher, DynamicDescriptorSets frameDescriptorSets)
    {
        Guid currentShader = Guid.Empty;
        //Guid currentMaterial = Guid.Empty;
        Guid currentMesh = Guid.Empty;
        VertexAttribute attributeMask = (VertexAttribute)(-1);
        uint indicesCount = 0;

        frameDescriptorSets.BindDescriptorSets(_graphCmdBuf);
        int firstInstance = 0;
        var renderList = batcher.RendGroups;
        var length = renderList.Length;

        for (int i = 0; i < length; firstInstance += renderList[i].count, i++)
        {
            var (rend, count) = renderList[i];
            if (!rend.IsValid)
                continue;

            var itemShader = rend._shader;
            var itemMesh = rend.mesh;

            if (itemShader.guid != currentShader) // shader switch
            {
                (var pipeline, attributeMask) = _renderAssets.GetPipelineAndAttributes(itemShader, batcher.PipelineLayout);
                _rendererBase.vk.CmdBindPipeline(_graphCmdBuf, PipelineBindPoint.Graphics, pipeline);
                currentShader = itemShader.guid;
            }

            // material switch?

            if (itemMesh.guid != currentMesh) // mesh switch
            {
                (var vertices, var indices, indicesCount) = _renderAssets.GetVertexIndexBuffersAndCount(itemMesh, attributeMask);

                if (vertices.Handle != default)
                    _rendererBase.vk.CmdBindVertexBuffers(_graphCmdBuf, 0, 1, vertices, 0);
                _rendererBase.vk.CmdBindIndexBuffer(_graphCmdBuf, indices, 0, IndexType.Uint32);
            }
            _rendererBase.vk.CmdDrawIndexed(_graphCmdBuf, indicesCount, (uint)count, 0, 0, (uint)firstInstance);
        }
    }

    private void EndRecordCommandBuffer()
    {
        _rendererBase.vk.CmdEndRenderPass(_graphCmdBuf);
        _ = _rendererBase.vk.EndCommandBuffer(_graphCmdBuf);
    }
}