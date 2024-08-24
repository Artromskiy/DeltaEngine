using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering.Internal;
using Delta.Runtime;
using Delta.Utilities;
using Silk.NET.SDL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Delta.Rendering;

internal partial class GraphicsModule : IGraphicsModule, IDisposable
{
    private readonly Api _api;
    private readonly unsafe Window* _window;
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

    //private readonly SceneBatcher _sceneBatcher;
    //private readonly TempRenderBatcher _gizmosBatcher;
    //private readonly TempRenderBatcher _instantBatcher;

    private Frame CurrentFrame => _frames.Peek();


    private readonly Fence _copyFence;
    private readonly Semaphore _copySemaphore;

    private CommandBuffer _copyCmdBuffer;

    public unsafe GraphicsModule(string appName)
    {
        _appName = appName;
        _api = new();
        _window = RenderHelper.CreateWindow(_api.sdl, _appName);
        RenderData = new RenderBase(_api, _window, _appName);
        _swapChain = new SwapChain(_api, RenderData, Buffering, RenderData.format);
        _renderAssets = new RenderAssets(RenderData);

        for (int i = 0; i < _swapChain.imageCount; i++)
            _frames.Enqueue(new Frame(RenderData, _renderAssets, _swapChain));

        _copyCmdBuffer = RenderHelper.CreateCommandBuffer(RenderData, RenderData.deviceQ.transferCmdPool);
        _copyFence = RenderHelper.CreateFence(RenderData, true);
        _copySemaphore = RenderHelper.CreateSemaphore(RenderData);

        //_sceneBatcher = new SceneBatcher();
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
        _api.sdl.PumpEvents();
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

        CurrentFrame.Draw(out var resize);
        if (resize)
            OnResize();
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
        RenderData.BeginCmdBuffer(_copyCmdBuffer);

        CurrentFrame.CopyBatchersData(_copyCmdBuffer);

        RenderData.EndCmdBuffer(RenderData.deviceQ.transferQueue, _copyCmdBuffer, _copyFence, _copySemaphore);

        CurrentFrame.AddSemaphore(_copySemaphore);
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

        _api.sdl.DestroyWindow(_window);
        //_api.sdl.Dispose();
    }

    private void OnResize()
    {
        _swapChain.Dispose();
        RenderData.UpdateSupportDetails();
        _swapChain = new SwapChain(_api, RenderData, 3, RenderData.format);

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

    public void DrawGizmos(Render render, Transform transform) => throw new NotImplementedException();
    public void DrawMesh(Render render, Transform transform) => throw new NotImplementedException();
}