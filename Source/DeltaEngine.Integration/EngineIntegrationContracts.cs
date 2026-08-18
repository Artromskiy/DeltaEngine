using System;
using System.Collections.Generic;

namespace DVG.Engine.Integration;

public enum EngineLifecycleStage
{
    InputInitialized,
    WorldInitialized,
    RenderInitialized,
    UiInitialized,
    FrameStarted,
    InputPolled,
    WorldUpdated,
    RenderUpdated,
    UiUpdated,
    FrameCompleted,
    ShutdownStarted,
    InputShutdown,
    WorldShutdown,
    RenderShutdown,
    UiShutdown,
    HostDisposalStarted,
    UiDisposed,
    RenderDisposed,
    WorldDisposed,
    InputDisposed,
    HostDisposed,
}

public readonly record struct EngineLifecycleEvent(EngineLifecycleStage Stage, int FrameNumber);

public readonly record struct InputSnapshot(int FrameNumber, bool ExitRequested = false);

public readonly record struct EngineFrameContext(int FrameNumber, float DeltaSeconds, InputSnapshot Input);

public interface IEngineHost : IDisposable
{
    bool IsRunning { get; }
    bool IsDisposed { get; }
    int CompletedFrames { get; }

    IReadOnlyList<EngineLifecycleEvent> StageLog { get; }

    void Start();
    void RunFrame(float deltaSeconds);
    void Shutdown();
}

public interface IEngineInputService : IDisposable
{
    void Initialize();
    InputSnapshot PollInput(int frameNumber, float deltaSeconds);
    void Shutdown();
}

public interface IEngineWorldService : IDisposable
{
    void Initialize();
    void Update(in EngineFrameContext context);
    void Shutdown();
}

public interface IEngineRenderService : IDisposable
{
    void Initialize();

    // Temporary adapter boundary. The permanent renderer input will be RenderPacket.
    // Render services must not poll or own input; input is supplied by the host.
    void Render(in EngineFrameContext context);
    void Shutdown();
}

public interface IEngineUiService : IDisposable
{
    void Initialize();
    void Update(in EngineFrameContext context);
    void Shutdown();
}
