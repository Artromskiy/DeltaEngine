using DVG.Engine.Integration;
using Xunit;

namespace DVG.Engine.Integration.Tests;

public sealed class EngineHostContractTests
{
    [Fact]
    public void Start_records_deterministic_initialization_order()
    {
        var host = new EngineHost(new FakeInputService(), new FakeWorldService(), new FakeRenderService(), new FakeUiService());

        host.Start();

        var stages = host.StageLog.Select(s => s.Stage).ToArray();
        Assert.Equal(
            new[]
            {
                EngineLifecycleStage.InputInitialized,
                EngineLifecycleStage.WorldInitialized,
                EngineLifecycleStage.RenderInitialized,
                EngineLifecycleStage.UiInitialized,
            },
            stages);
    }

    [Fact]
    public void Start_is_idempotent()
    {
        var host = new EngineHost(new FakeInputService(), new FakeWorldService(), new FakeRenderService(), new FakeUiService());

        host.Start();
        host.Start();

        Assert.Equal(4, host.StageLog.Count);
    }

    [Fact]
    public void RunFrame_records_deterministic_stage_order()
    {
        var host = new EngineHost(new FakeInputService(), new FakeWorldService(), new FakeRenderService(), new FakeUiService());

        host.Start();
        host.RunFrame(0.016f);

        var stages = host.StageLog.Select(s => s.Stage).ToArray();
        Assert.Equal(
            new[]
            {
                EngineLifecycleStage.InputInitialized,
                EngineLifecycleStage.WorldInitialized,
                EngineLifecycleStage.RenderInitialized,
                EngineLifecycleStage.UiInitialized,
                EngineLifecycleStage.FrameStarted,
                EngineLifecycleStage.InputPolled,
                EngineLifecycleStage.WorldUpdated,
                EngineLifecycleStage.RenderUpdated,
                EngineLifecycleStage.UiUpdated,
                EngineLifecycleStage.FrameCompleted,
            },
            stages);
    }

    [Fact]
    public void Shutdown_records_expected_order()
    {
        var host = new EngineHost(new FakeInputService(), new FakeWorldService(), new FakeRenderService(), new FakeUiService());

        host.Start();
        host.Shutdown();

        var stages = host.StageLog.Select(s => s.Stage).ToArray();
        Assert.Equal(
            new[]
            {
                EngineLifecycleStage.InputInitialized,
                EngineLifecycleStage.WorldInitialized,
                EngineLifecycleStage.RenderInitialized,
                EngineLifecycleStage.UiInitialized,
                EngineLifecycleStage.ShutdownStarted,
                EngineLifecycleStage.InputShutdown,
                EngineLifecycleStage.WorldShutdown,
                EngineLifecycleStage.RenderShutdown,
                EngineLifecycleStage.UiShutdown,
            },
            stages);
    }

    [Fact]
    public void RunFrame_propagates_service_exception_without_swallowing()
    {
        var world = new FakeWorldService { ThrowOnUpdate = true };
        var host = new EngineHost(new FakeInputService(), world, new FakeRenderService(), new FakeUiService());

        host.Start();
        var exception = Assert.Throws<InvalidOperationException>(() => host.RunFrame(0.016f));

        Assert.Equal("World update failure", exception.Message);
        Assert.Equal(0, host.CompletedFrames);
        Assert.Equal(
            new[]
            {
                EngineLifecycleStage.InputInitialized,
                EngineLifecycleStage.WorldInitialized,
                EngineLifecycleStage.RenderInitialized,
                EngineLifecycleStage.UiInitialized,
                EngineLifecycleStage.FrameStarted,
                EngineLifecycleStage.InputPolled,
                EngineLifecycleStage.WorldUpdated,
            },
            host.StageLog.Select(s => s.Stage).ToArray());
    }

    [Fact]
    public void Shutdown_is_idempotent()
    {
        var host = new EngineHost(new FakeInputService(), new FakeWorldService(), new FakeRenderService(), new FakeUiService());

        host.Start();
        host.Shutdown();
        var stageCount = host.StageLog.Count;

        host.Shutdown();

        Assert.Equal(stageCount, host.StageLog.Count);
        Assert.False(host.IsRunning);
    }

    [Fact]
    public void RunFrame_rejects_invalid_delta_time()
    {
        var host = new EngineHost(new FakeInputService(), new FakeWorldService(), new FakeRenderService(), new FakeUiService());
        host.Start();

        Assert.Throws<ArgumentOutOfRangeException>(() => host.RunFrame(float.NaN));
        Assert.Equal(4, host.StageLog.Count);
    }

    [Fact]
    public void Dispose_records_shutdown_and_disposal_order()
    {
        var host = new EngineHost(new FakeInputService(), new FakeWorldService(), new FakeRenderService(), new FakeUiService());
        host.Start();
        host.Dispose();
        var stageCount = host.StageLog.Count;
        host.Dispose();

        Assert.Equal(stageCount, host.StageLog.Count);
        var stages = host.StageLog.Select(s => s.Stage).ToArray();
        Assert.Equal(
            new[]
            {
                EngineLifecycleStage.InputInitialized,
                EngineLifecycleStage.WorldInitialized,
                EngineLifecycleStage.RenderInitialized,
                EngineLifecycleStage.UiInitialized,
                EngineLifecycleStage.ShutdownStarted,
                EngineLifecycleStage.InputShutdown,
                EngineLifecycleStage.WorldShutdown,
                EngineLifecycleStage.RenderShutdown,
                EngineLifecycleStage.UiShutdown,
                EngineLifecycleStage.HostDisposalStarted,
                EngineLifecycleStage.UiDisposed,
                EngineLifecycleStage.RenderDisposed,
                EngineLifecycleStage.WorldDisposed,
                EngineLifecycleStage.InputDisposed,
                EngineLifecycleStage.HostDisposed,
            },
            stages);
    }

    private sealed class FakeInputService : IEngineInputService
    {
        public void Initialize() {}

        public InputSnapshot PollInput(int frameNumber, float deltaSeconds)
        {
            return new InputSnapshot(frameNumber);
        }

        public void Shutdown() {}

        public void Dispose() {}
    }

    private sealed class FakeWorldService : IEngineWorldService
    {
        public bool ThrowOnUpdate { get; init; }

        public void Initialize() {}

        public void Update(in EngineFrameContext context)
        {
            if (ThrowOnUpdate)
                throw new InvalidOperationException("World update failure");
        }

        public void Shutdown() {}

        public void Dispose() {}
    }

    private sealed class FakeRenderService : IEngineRenderService
    {
        public void Initialize() {}

        public void Render(in EngineFrameContext context) {}

        public void Shutdown() {}

        public void Dispose() {}
    }

    private sealed class FakeUiService : IEngineUiService
    {
        public void Initialize() {}

        public void Update(in EngineFrameContext context) {}

        public void Shutdown() {}

        public void Dispose() {}
    }
}
