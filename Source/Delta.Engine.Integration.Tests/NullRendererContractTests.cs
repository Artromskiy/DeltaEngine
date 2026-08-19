using Delta.Engine.Integration;
using Xunit;

namespace Delta.Engine.Integration.Tests;

public sealed class NullRendererContractTests
{
    [Fact]
    public void HeadlessLoopAcceptsResizeAndNoOpFrame()
    {
        using var renderer = new NullRenderer();
        var surface = new EngineRenderSurface(320, 200);
        var frame = new EngineRenderFrame(4, 1f / 60f, surface);

        renderer.Resize(in surface);
        renderer.Render(in frame);

        Assert.False(renderer.HasBackend);
        Assert.Equal("none", renderer.BackendName);
        Assert.Equal(1, renderer.ResizeCount);
        Assert.Equal(1, renderer.RenderCount);
        Assert.Equal(frame, renderer.LastFrame);
    }

    [Fact]
    public void DisposeIsIdempotentAndRemovesBackend()
    {
        var renderer = new NullRenderer();

        renderer.Dispose();
        renderer.Dispose();

        Assert.True(renderer.IsDisposed);
        Assert.Throws<ObjectDisposedException>(() => renderer.Render(default));
    }

    [Fact]
    public void InvalidResizeRemainsBackendFree()
    {
        using var renderer = new NullRenderer();
        var surface = new EngineRenderSurface(0, 0, false);

        renderer.Resize(in surface);

        Assert.False(renderer.LastSurface.IsValid);
        Assert.Equal(0, renderer.LastSurface.Width);
        Assert.Equal(0, renderer.LastSurface.Height);
    }
}
