using Delta.Engine.Integration;
using Xunit;

namespace Delta.Engine.Integration.Tests;

public sealed class UiFrameSourceContractTests
{
    [Fact]
    public void Optional_ui_frame_source_is_prepared_before_render()
    {
        var events = new List<string>();
        var ui = new FakeUi(events);
        var render = new FakeRender(events, ui);
        using var host = new EngineHost(new FakeInput(), new FakeWorld(), render, ui);

        host.Start();
        host.RunFrame(0.016f);

        Assert.Equal(new[] { "ui", "render:1" }, events);
    }

    private sealed class FakeInput : IEngineInputService
    {
        public void Initialize() { }
        public InputSnapshot PollInput(int frameNumber, float deltaSeconds) => new(frameNumber);
        public void Shutdown() { }
        public void Dispose() { }
    }

    private sealed class FakeWorld : IEngineWorldService
    {
        public void Initialize() { }
        public void Update(in EngineFrameContext context) { }
        public void Shutdown() { }
        public void Dispose() { }
    }

    private sealed class FakeUi(List<string> events) : IEngineUiFrameSource
    {
        public ReadOnlyMemory<EngineUiQuad> CurrentDrawList => new[] { new EngineUiQuad(0, 0, 1, 1, 1, 1, 1, 1) };
        public void Initialize() { }
        public void PrepareFrame(in EngineFrameContext context) => events.Add("ui");
        public void Update(in EngineFrameContext context) => throw new InvalidOperationException("Host should use PrepareFrame.");
        public void Shutdown() { }
        public void Dispose() { }
    }

    private sealed class FakeRender(List<string> events, IEngineUiDrawListProvider ui) : IEngineRenderService
    {
        public void Initialize() { }
        public void Render(in EngineFrameContext context) => events.Add($"render:{ui.CurrentDrawList.Length}");
        public void Shutdown() { }
        public void Dispose() { }
    }
}
