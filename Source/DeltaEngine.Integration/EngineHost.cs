using System;
using System.Collections.Generic;

namespace DVG.Engine.Integration;

public sealed class EngineHost(
    IEngineInputService inputService,
    IEngineWorldService worldService,
    IEngineRenderService renderService,
    IEngineUiService uiService) : IEngineHost
{
    private readonly IEngineInputService _inputService = inputService;
    private readonly IEngineWorldService _worldService = worldService;
    private readonly IEngineRenderService _renderService = renderService;
    private readonly IEngineUiService _uiService = uiService;
    private readonly List<EngineLifecycleEvent> _stageLog = [];

    private int _nextFrameNumber;
    private int _completedFrames;
    private bool _isDisposed;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public bool IsDisposed => _isDisposed;
    public int CompletedFrames => _completedFrames;
    public IReadOnlyList<EngineLifecycleEvent> StageLog => _stageLog;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (_isRunning)
            return;

        AddStage(EngineLifecycleStage.InputInitialized, -1);
        _inputService.Initialize();

        AddStage(EngineLifecycleStage.WorldInitialized, -1);
        _worldService.Initialize();

        AddStage(EngineLifecycleStage.RenderInitialized, -1);
        _renderService.Initialize();

        AddStage(EngineLifecycleStage.UiInitialized, -1);
        _uiService.Initialize();

        _isRunning = true;
    }

    public void RunFrame(float deltaSeconds)
    {
        ValidateFrameCanRun();
        if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta time must be finite and non-negative.");

        var frameNumber = _nextFrameNumber++;

        AddStage(EngineLifecycleStage.FrameStarted, frameNumber);
        var input = _inputService.PollInput(frameNumber, deltaSeconds);
        AddStage(EngineLifecycleStage.InputPolled, frameNumber);

        var context = new EngineFrameContext(frameNumber, deltaSeconds, input);

        AddStage(EngineLifecycleStage.WorldUpdated, frameNumber);
        _worldService.Update(context);

        AddStage(EngineLifecycleStage.RenderUpdated, frameNumber);
        _renderService.Render(context);

        AddStage(EngineLifecycleStage.UiUpdated, frameNumber);
        _uiService.Update(context);

        AddStage(EngineLifecycleStage.FrameCompleted, frameNumber);
        _completedFrames++;

        if (input.ExitRequested)
            Shutdown();
    }

    public void Shutdown()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (!_isRunning)
            return;

        _isRunning = false;

        AddStage(EngineLifecycleStage.ShutdownStarted, -1);

        AddStage(EngineLifecycleStage.InputShutdown, -1);
        _inputService.Shutdown();

        AddStage(EngineLifecycleStage.WorldShutdown, -1);
        _worldService.Shutdown();

        AddStage(EngineLifecycleStage.RenderShutdown, -1);
        _renderService.Shutdown();

        AddStage(EngineLifecycleStage.UiShutdown, -1);
        _uiService.Shutdown();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        if (_isRunning)
            Shutdown();

        _isDisposed = true;
        AddStage(EngineLifecycleStage.HostDisposalStarted, -1);

        _uiService.Dispose();
        AddStage(EngineLifecycleStage.UiDisposed, -1);

        _renderService.Dispose();
        AddStage(EngineLifecycleStage.RenderDisposed, -1);

        _worldService.Dispose();
        AddStage(EngineLifecycleStage.WorldDisposed, -1);

        _inputService.Dispose();
        AddStage(EngineLifecycleStage.InputDisposed, -1);
        AddStage(EngineLifecycleStage.HostDisposed, -1);
    }

    private void ValidateFrameCanRun()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        if (!_isRunning)
            throw new InvalidOperationException("EngineHost must be started before frames are run.");
    }

    private void AddStage(EngineLifecycleStage stage, int frameNumber)
    {
        _stageLog.Add(new EngineLifecycleEvent(stage, frameNumber));
    }
}
