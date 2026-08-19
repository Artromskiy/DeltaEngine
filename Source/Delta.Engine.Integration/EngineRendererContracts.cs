namespace Delta.Engine.Integration;

public readonly record struct EngineRenderSurface(
    int Width,
    int Height,
    bool IsValid = true)
{
    public static EngineRenderSurface Empty => new(0, 0, false);
}

public readonly record struct EngineRenderFrame(
    long FrameNumber,
    float DeltaSeconds,
    EngineRenderSurface Surface);

public interface IRenderFrameSink : IDisposable
{
    void Resize(in EngineRenderSurface surface);

    void Render(in EngineRenderFrame frame);
}

public interface IRenderer : IRenderFrameSink
{
}

/// <summary>
/// A deterministic backend-free renderer for headless engine/editor/game runs.
/// It records lifecycle observations for diagnostics but performs no rendering.
/// </summary>
public sealed class NullRenderer : IRenderer
{
    private bool _disposed;

    public string BackendName => "none";

    public bool HasBackend => false;

    public bool IsDisposed => _disposed;

    public EngineRenderSurface LastSurface { get; private set; } = EngineRenderSurface.Empty;

    public EngineRenderFrame? LastFrame { get; private set; }

    public int ResizeCount { get; private set; }

    public int RenderCount { get; private set; }

    public void Resize(in EngineRenderSurface surface)
    {
        ThrowIfDisposed();
        LastSurface = surface;
        ResizeCount++;
    }

    public void Render(in EngineRenderFrame frame)
    {
        ThrowIfDisposed();
        LastFrame = frame;
        RenderCount++;
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
