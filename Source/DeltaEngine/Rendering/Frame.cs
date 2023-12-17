using Silk.NET.Vulkan;
using System;
using System.Diagnostics;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace DeltaEngine.Rendering;
internal class Frame : IDisposable
{
    private readonly RenderBase _rendererData;
    private SwapChain _swapChain;

    private RenderPass _renderPass;

    private Semaphore imageAvailable;
    private Semaphore renderFinished;
    private Fence renderFinishedFence;

    private CommandBuffer commandBuffer;
    private DescriptorSet matrices;

    private readonly DynamicBuffer _matricesDynamicBuffer;
    private Semaphore _syncSemaphore;
    private Buffer _vertexBuffer;
    private Buffer _indexBuffer;
    private uint _indicesLength;
    private uint _verticesLength;
    private uint _instances;

    public void UpdateSwapChain(SwapChain swapChain)
    {
        _swapChain = swapChain;
    }

    public unsafe Frame(RenderBase renderBase, SwapChain swapChain, RenderPass renderPass, DescriptorSetLayout descriptorSetLayout)
    {
        _rendererData = renderBase;
        _swapChain = swapChain;
        _renderPass = renderPass;
        _matricesDynamicBuffer = new(renderBase, 1);

        FenceCreateInfo fenceInfo = new(StructureType.FenceCreateInfo, null, FenceCreateFlags.SignaledBit);
        SemaphoreCreateInfo semaphoreInfo = new(StructureType.SemaphoreCreateInfo);
        matrices = RenderHelper.CreateDescriptorSet(renderBase, descriptorSetLayout);

        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _rendererData.deviceQueues.graphicsCmdPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        _ = _rendererData.vk.AllocateCommandBuffers(_rendererData.deviceQueues.device, allocInfo, out commandBuffer);
        _ = _rendererData.vk.CreateSemaphore(_rendererData.deviceQueues.device, semaphoreInfo, null, out imageAvailable);
        _ = _rendererData.vk.CreateSemaphore(_rendererData.deviceQueues.device, semaphoreInfo, null, out renderFinished);
        _ = _rendererData.vk.CreateFence(_rendererData.deviceQueues.device, fenceInfo, null, out renderFinishedFence);
    }

    public unsafe void Dispose()
    {
        _rendererData.vk.FreeCommandBuffers(_rendererData.deviceQueues.device, _rendererData.deviceQueues.graphicsCmdPool, 1, in commandBuffer);
        _rendererData.vk.DestroySemaphore(_rendererData.deviceQueues.device, imageAvailable, null);
        _rendererData.vk.DestroySemaphore(_rendererData.deviceQueues.device, renderFinished, null);
        _rendererData.vk.DestroyFence(_rendererData.deviceQueues.device, renderFinishedFence, null);
    }

    public void Sync()
    {
        _rendererData.vk.WaitForFences(_rendererData.deviceQueues.device, 1, renderFinishedFence, true, ulong.MaxValue);
    }

    public bool Synced() => _rendererData.vk.GetFenceStatus(_rendererData.deviceQueues.device, renderFinishedFence) == Result.Success;

    public DynamicBuffer GetTRSBuffer() => _matricesDynamicBuffer;
    public void SetBuffers(Buffer vbo, Buffer ibo, uint indices, uint vertices)
    {
        _vertexBuffer = vbo;
        _indexBuffer = ibo;
        _indicesLength = indices;
        _verticesLength = vertices;
    }

    public void SetInstanceCount(uint instances)
    {
        _instances = instances;
    }

    public void AddSemaphore(Semaphore semaphore)
    {
        _syncSemaphore = semaphore;
    }

    private readonly Stopwatch _acquire = new();
    private readonly Stopwatch _recordRender = new();
    private readonly Stopwatch _submitRender = new();
    private readonly Stopwatch _submitPresent = new();
    public TimeSpan AcquireMetric => _acquire.Elapsed;
    public TimeSpan RecordMetric => _recordRender.Elapsed;
    public TimeSpan SubmitDrawMetric => _submitRender.Elapsed;
    public TimeSpan SubmitPresentMetric => _submitPresent.Elapsed;
    public void ClearMetrics()
    {
        _acquire.Reset();
        _recordRender.Reset();
        _submitRender.Reset();
        _submitPresent.Reset();
    }

    public unsafe void Draw(Pipeline graphicsPipeline, PipelineLayout layout, out bool resize)
    {
        //_rendererData.vk.WaitForFences(_rendererData.deviceQueues.device, 1, renderFinishedFence, true, ulong.MaxValue);

        var imageAvailable = this.imageAvailable;
        uint imageIndex = 0;

        _acquire.Start();
        var res = _swapChain.khrSw.AcquireNextImage(_rendererData.deviceQueues.device, _swapChain.swapChain, ulong.MaxValue, imageAvailable, default, &imageIndex);
        _acquire.Stop();

        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
        if (resize)
            return;

        _rendererData.vk.ResetFences(_rendererData.deviceQueues.device, 1, renderFinishedFence);
        _rendererData.vk.ResetCommandBuffer(commandBuffer, 0);

        if (_matricesDynamicBuffer.ChangedBuffer)
        {
            RenderHelper.BindBuffersToDescriptorSet(_rendererData, matrices, _matricesDynamicBuffer.GetBuffer(), 0, DescriptorType.StorageBuffer);
            _matricesDynamicBuffer.ChangedBuffer = false;
        }
        _recordRender.Start();
        RecordCommandBuffer(commandBuffer, imageIndex, graphicsPipeline, layout, _vertexBuffer, _indexBuffer, _indicesLength);
        _recordRender.Stop();

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
        _submitRender.Start();
        _ = _rendererData.vk.QueueSubmit(_rendererData.deviceQueues.graphicsQueue, 1, submitInfo, renderFinishedFence);
        _submitRender.Stop();
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
        _submitPresent.Start();
        res = _swapChain.khrSw.QueuePresent(_rendererData.deviceQueues.presentQueue, presentInfo);
        _submitRender.Stop();
        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
        if (!resize)
            _ = res;
    }

    private unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex, Pipeline graphicsPipeline, PipelineLayout layout, Buffer vbo, Buffer ibo, uint indices)
    {
        CommandBufferBeginInfo beginInfo = new(StructureType.CommandBufferBeginInfo);
        ClearValue clearColor = new(new ClearColorValue(0.05f, 0.05f, 0.05f, 1));
        Rect2D renderRect = new(null, _swapChain.extent);
        var renderPass = _renderPass;
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
        var matrices = this.matrices;
        _rendererData.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, layout, 0, 1, &matrices, 0, 0);

        _rendererData.vk.CmdDrawIndexed(commandBuffer, _indicesLength, _instances, 0, 0, 0);
        //_rendererData.vk.CmdDraw(commandBuffer, _verticesLength, _instances, 0, 0);
        _rendererData.vk.CmdEndRenderPass(commandBuffer);

        _ = _rendererData.vk.EndCommandBuffer(commandBuffer);
    }
}
