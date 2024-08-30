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

    private Semaphore _imageAvailable;
    private Semaphore _renderFinished;
    private Fence _renderFinishedFence;

    private CommandBuffer _commandBuffer;

    private readonly Dictionary<IRenderBatcher, DescriptorSets> _batchedSets = [];

    //public readonly DescriptorSets DescriptorSets;


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

        //DescriptorSets = new DescriptorSets(renderBase);

        FenceCreateInfo fenceInfo = new(StructureType.FenceCreateInfo, null, FenceCreateFlags.SignaledBit);
        SemaphoreCreateInfo semaphoreInfo = new(StructureType.SemaphoreCreateInfo);

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _rendererBase.deviceQ.graphicsCmdPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        _ = _rendererBase.vk.AllocateCommandBuffers(_rendererBase.deviceQ, allocInfo, out _commandBuffer);
        _ = _rendererBase.vk.CreateSemaphore(_rendererBase.deviceQ, semaphoreInfo, null, out _imageAvailable);
        _ = _rendererBase.vk.CreateSemaphore(_rendererBase.deviceQ, semaphoreInfo, null, out _renderFinished);
        _ = _rendererBase.vk.CreateFence(_rendererBase.deviceQ, fenceInfo, null, out _renderFinishedFence);
    }

    public unsafe void Dispose()
    {
        _rendererBase.vk.FreeCommandBuffers(_rendererBase.deviceQ, _rendererBase.deviceQ.graphicsCmdPool, 1, in _commandBuffer);
        _rendererBase.vk.DestroySemaphore(_rendererBase.deviceQ, _imageAvailable, null);
        _rendererBase.vk.DestroySemaphore(_rendererBase.deviceQ, _renderFinished, null);
        _rendererBase.vk.DestroyFence(_rendererBase.deviceQ, _renderFinishedFence, null);

        foreach (var item in _batchedSets)
            item.Value.Dispose();
        _batchedSets.Clear();
        //DescriptorSets.Dispose();
    }
    public void CopyBatchersData(CommandBuffer commandBuffer)
    {
        foreach (var item in _batchedSets)
            item.Value.CopyBatcherData(item.Key, commandBuffer);
    }
    public void Sync() => _rendererBase.vk.WaitForFences(_rendererBase.deviceQ, 1, _renderFinishedFence, true, ulong.MaxValue);
    public bool Synced() => _rendererBase.vk.GetFenceStatus(_rendererBase.deviceQ, _renderFinishedFence) == Result.Success;
    public void AddSemaphore(Semaphore semaphore) => _syncSemaphore = semaphore;
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

    public unsafe void Draw(out bool resize)
    {
        //_rendererData.vk.WaitForFences(_rendererData.deviceQueues.device, 1, renderFinishedFence, true, ulong.MaxValue);

        var imageAvailable = _imageAvailable;
        uint imageIndex = 0;

        var res = Result.Success; // _swapChain.khrSw.AcquireNextImage(_rendererBase.deviceQ, _swapChain.swapChain, ulong.MaxValue, imageAvailable, default, &imageIndex);

        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
        if (resize)
            return;

        _rendererBase.vk.ResetFences(_rendererBase.deviceQ, 1, _renderFinishedFence);
        _rendererBase.vk.ResetCommandBuffer(_commandBuffer, 0);

        foreach (var item in _batchedSets)
            item.Value.UpdateDescriptorSets();
        BeginRecordCommandBuffer(imageIndex);
        foreach (var item in _batchedSets)
            RecordCommandBuffer(item.Value);
        EndRecordCommandBuffer();

        var buffer = _commandBuffer;
        var syncSemaphore = _syncSemaphore;

        var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.ColorAttachmentOutputBit };
        var waitBeforeRender = stackalloc Semaphore[2] { imageAvailable, syncSemaphore };
        var renderFinished = this._renderFinished;

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
        _ = _rendererBase.vk.QueueSubmit(_rendererBase.deviceQ.graphicsQueue, 1, submitInfo, _renderFinishedFence);
        /*
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
        */
        res = Result.Success; // _swapChain.khrSw.QueuePresent(_rendererBase.deviceQ.presentQueue, presentInfo);
        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
        if (!resize)
            _ = res;
    }

    private unsafe void BeginRecordCommandBuffer(uint imageIndex)
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