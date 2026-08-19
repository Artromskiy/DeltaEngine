using System;
using Delta.Engine.Integration;

namespace Delta.Engine.Rendering;

public sealed class NullGraphicsModule : Delta.Engine.Runtime.IGraphicsModule
{
    private readonly IRenderer _renderer;
    private bool _disposed;
    private long _frameNumber;
    private (int width, int height) _size;

    public NullGraphicsModule(string appName, IRenderer? renderer = null)
    {
        _ = appName;
        _renderer = renderer ?? new NullRenderer();
    }

    public IRenderer Renderer => _renderer;

    public (int width, int height) Size
    {
        get => _size;
        set => Resize(value.width, value.height);
    }

    public void Resize(int width, int height)
    {
        ThrowIfDisposed();
        _size = (width, height);
        var surface = new EngineRenderSurface(width, height, width > 0 && height > 0);
        _renderer.Resize(in surface);
    }

    public void Execute(float deltaSeconds = 0)
    {
        ThrowIfDisposed();
        var surface = new EngineRenderSurface(_size.width, _size.height, _size.width > 0 && _size.height > 0);
        var frame = new EngineRenderFrame(_frameNumber++, deltaSeconds, surface);
        _renderer.Render(in frame);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _renderer.Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
