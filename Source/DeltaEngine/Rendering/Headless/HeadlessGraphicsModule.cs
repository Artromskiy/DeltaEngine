using Delta.ECS.Components;
using Delta.Rendering.Internal;
using Delta.Runtime;
using Delta.Utilities;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace Delta.Rendering.Headless;
internal class HeadlessGraphicsModule : IGraphicsModule, IDisposable
{
    public RenderBase RenderData { get; private set; }

    private readonly string _appName;

    private SwapChain _swapChain;

    private readonly RenderAssets _renderAssets;

    private readonly Queue<Frame> _frames = [];

    private const uint Buffering = 3;
    private const bool CanSkipRender = false;
    private const bool RenderLessMode = false;

    private bool _skippedFrame = true;

    private readonly HashSet<IRenderBatcher> _renderBatchers = [];

    private Frame CurrentFrame => _frames.Peek();

    public Memory<byte> RenderStream => _swapChain.RenderStream;


    private readonly Fence _copyFence;
    private readonly Semaphore _copySemaphore;

    private CommandBuffer _copyCmdBuffer;

    public unsafe HeadlessGraphicsModule(string appName)
    {
        _appName = appName;
        RenderData = new RenderBase(_appName);
        RenderData.Init();
        _swapChain = new SwapChain(RenderData, Buffering, RenderData.Format, 1024, 1024);
        _renderAssets = new RenderAssets(RenderData);

        for (int i = 0; i < _swapChain.imageCount; i++)
            _frames.Enqueue(new Frame(RenderData, _renderAssets, _swapChain));

        _copyCmdBuffer = RenderHelper.CreateCommandBuffer(RenderData.vk, RenderData.deviceQ, RenderData.deviceQ.GetCmdPool(QueueType.Transfer));
        _copyFence = RenderHelper.CreateFence(RenderData.vk, RenderData.deviceQ, true);
        _copySemaphore = RenderHelper.CreateSemaphore(RenderData.vk, RenderData.deviceQ);
    }

    public void AddRenderBatcher(IRenderBatcher renderBatcher)
    {
        _renderBatchers.Add(renderBatcher);
        foreach (var frame in _frames)
            frame.AddBatcher(renderBatcher);
    }

    public void RemoveRenderBatcher(IRenderBatcher renderBatcher)
    {
        _renderBatchers.Remove(renderBatcher);
        foreach (var frame in _frames)
            frame.RemoveBatcher(renderBatcher);
    }

    public void Execute()
    {
        Sync();
        Draw();
    }

    private void Sync()
    {
        if (!_skippedFrame)
            _frames.Enqueue(_frames.Dequeue());

        PreSync();

        if (_skippedFrame = RenderLessMode || (CanSkipRender && !CurrentFrame.Synced()))
            return;

        CurrentFrame.Sync();

        PostSync();
    }

    private void Draw()
    {
        if (_skippedFrame)
            return;
        CurrentFrame.Draw();
    }

    /// <summary>
    /// Uploads data from game context to global graphics buffers using <see cref="IRenderBatcher"/>
    /// </summary>
    private void PreSync()
    {
        RenderData.vk.WaitForFences(RenderData.deviceQ, 1, _copyFence, true, ulong.MaxValue);
        RenderData.vk.ResetCommandBuffer(_copyCmdBuffer, 0);
        foreach (var item in _renderBatchers)
            item.Execute();
    }

    /// <summary>
    /// Uploads data from global graphics buffers to appropriate frame buffer asyncronously
    /// </summary>
    private void PostSync()
    {
        RenderData.vk.ResetFences(RenderData.deviceQ, 1, _copyFence);
        RenderHelper.BeginCmdBuffer(RenderData.vk, _copyCmdBuffer);

        CurrentFrame.CopyBatchersData(_copyCmdBuffer);

        RenderHelper.EndCmdBuffer(RenderData.vk, RenderData.deviceQ.GetQueue(QueueType.Transfer), _copyCmdBuffer, _copyFence, _copySemaphore);

        CurrentFrame.AddSemaphore(_copySemaphore);
    }

    public (int width, int height) Size
    {
        get => (_swapChain.width, _swapChain.height);
        set => SetSize(value.width, value.height);
    }

    private void SetSize(int width, int height)
    {
        if (_swapChain.width == width && _swapChain.height == height)
            return;
        _swapChain.Dispose();
        _swapChain = new SwapChain(RenderData, 3, RenderData.Format, width, height);

        if (_swapChain.imageCount == _frames.Count)
        {
            foreach (var frame in _frames)
                frame.UpdateSwapChain(_swapChain);
            return;
        }

        _frames.Dispose();
        _frames.Clear();
        for (int i = 0; i < _swapChain.imageCount; i++)
            _frames.Enqueue(new Frame(RenderData, _renderAssets, _swapChain));
    }

    public unsafe void Dispose()
    {
        _ = RenderData.vk.DeviceWaitIdle(RenderData.deviceQ);

        _frames.Dispose();

        foreach (var item in _renderBatchers)
            item.Dispose();
        _renderBatchers.Clear();

        _renderAssets.Dispose();

        _swapChain.Dispose();
        RenderData.Dispose();
    }

    public void DrawGizmos(Render render, Transform transform) => throw new NotImplementedException();
    public void DrawMesh(Render render, Transform transform) => throw new NotImplementedException();
}