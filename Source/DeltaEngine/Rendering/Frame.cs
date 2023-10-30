using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using static DeltaEngine.DebugHelper;
using System;

namespace DeltaEngine.Rendering;
internal class Frame: IDisposable
{
    private readonly RenderBase _rendererData;
    private SwapChain _swapChain;

    private Semaphore imageAvailable;
    private Semaphore renderFinished;
    private Fence inFlight;

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
        SemaphoreCreateInfo semaphoreInfo = new();

        CommandBufferAllocateInfo allocInfo = new()
        {
            CommandPool = _rendererData.commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };

        _ = _rendererData.vk.AllocateCommandBuffers(_rendererData.device, allocInfo, out commandBuffer);
        _ = _rendererData.vk.CreateSemaphore(_rendererData.device, semaphoreInfo, null, out imageAvailable);
        _ = _rendererData.vk.CreateSemaphore(_rendererData.device, semaphoreInfo, null, out renderFinished);
        _ = _rendererData.vk.CreateFence(_rendererData.device, fenceInfo, null, out inFlight);
    }

    public unsafe void Dispose()
    {
        _rendererData.vk.FreeCommandBuffers(_rendererData.device, _rendererData.commandPool, new Span<CommandBuffer>(ref commandBuffer));
        _rendererData.vk.DestroySemaphore(_rendererData.device, imageAvailable, null);
        _rendererData.vk.DestroySemaphore(_rendererData.device, renderFinished, null);
        _rendererData.vk.DestroyFence(_rendererData.device, inFlight, null);
    }

    public unsafe void Draw(RenderPass renderPass, Pipeline graphicsPipeline, Buffer vbo, Buffer ibo, uint indices, out bool resize)
    {
        _rendererData.vk.WaitForFences(_rendererData.device, 1, inFlight, true, ulong.MaxValue);
        var waitSemaphores = stackalloc[] { imageAvailable };
        uint imageIndex = 0;
        var res = _swapChain.khrSw.AcquireNextImage(_rendererData.device, _swapChain.swapChain, ulong.MaxValue, *waitSemaphores, default, &imageIndex);

        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;

        if (resize)
            return;

        _rendererData.vk.ResetFences(_rendererData.device, 1, inFlight);
        _rendererData.vk.ResetCommandBuffer(commandBuffer, 0);

        RecordCommandBuffer(commandBuffer, imageIndex, renderPass, graphicsPipeline, vbo, ibo, indices);

        var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };
        var signalSemaphores = stackalloc[] { renderFinished };
        var buffer = commandBuffer;
        SubmitInfo submitInfo = new()
        {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores,
        };
        _ = _rendererData.vk.QueueSubmit(_rendererData.graphicsQueue, 1, submitInfo, inFlight);
        var swapChains = stackalloc[] { _swapChain.swapChain };
        PresentInfoKHR presentInfo = new()
        {
            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,
            SwapchainCount = 1,
            PSwapchains = swapChains,
            PImageIndices = &imageIndex
        };
        res = _swapChain.khrSw.QueuePresent(_rendererData.presentQueue, presentInfo);
        resize = res == Result.SuboptimalKhr || res == Result.ErrorOutOfDateKhr;
    }


    private unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex, RenderPass renderPass, Pipeline graphicsPipeline, Buffer vbo, Buffer ibo, uint indices)
    {
        CommandBufferBeginInfo beginInfo = new(StructureType.CommandBufferBeginInfo);
        ClearValue clearColor = new(new ClearColorValue(0.05f, 0.05f, 0.05f, 1));

        RenderPassBeginInfo renderPassInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderPass,
            Framebuffer = _swapChain.frameBuffers[(int)imageIndex],
            ClearValueCount = 1,
            PClearValues = &clearColor,
            RenderArea = new Rect2D(extent: _swapChain.extent)
        };

        Viewport viewport = new()
        {
            Width = _swapChain.extent.Width,
            Height = _swapChain.extent.Height,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };

        Rect2D scissor = new(extent: _swapChain.extent);

        _ = _rendererData.vk.BeginCommandBuffer(commandBuffer, &beginInfo);

        _rendererData.vk.CmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);
        _rendererData.vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, graphicsPipeline);
        _rendererData.vk.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        _rendererData.vk.CmdSetScissor(commandBuffer, 0, 1, &scissor);

        var buffer = stackalloc Buffer[] { vbo };
        ulong offsets = 0;

        _rendererData.vk.CmdBindVertexBuffers(commandBuffer, 0, 1, buffer, &offsets);
        _rendererData.vk.CmdBindIndexBuffer(commandBuffer, ibo, 0, IndexType.Uint32);
        _rendererData.vk.CmdDrawIndexed(commandBuffer, indices, 1, 0, 0, 0);
        _rendererData.vk.CmdEndRenderPass(commandBuffer);

        _ = _rendererData.vk.EndCommandBuffer(commandBuffer);
    }
}
