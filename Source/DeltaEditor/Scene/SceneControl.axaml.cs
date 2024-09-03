using Avalonia;
using Avalonia.Threading;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Delta.Runtime;
using System;
using System.IO;

namespace DeltaEditor;

public partial class SceneControl : UserControl
{
    private WriteableBitmap? _bitmap;
    private WriteableBitmap? _prevBitmap;
    public SceneControl()
    {
        InitializeComponent();
    }

    public void UpdateScene(IRuntimeContext ctx)
    {
        var bounds = RenderBorder.Bounds;

        if (!SizeIsValid(bounds.Width, bounds.Height))
            return;

        var w = (int)bounds.Width;
        var h = (int)bounds.Height;

        if (!ResizeBitmap(w, h))
        {
            _prevBitmap?.Dispose();
            _prevBitmap = null;
            WriteBitmap(ctx, _bitmap!);
            Render.InvalidateVisual();
        }
        else
        {
            ctx.GraphicsModule.Size = (w, h);
        }
    }

    private static bool SizeIsValid(double width, double height)
    {
        return width != 0 && height != 0 && double.IsNormal(width) && double.IsNormal(height);
    }

    private static unsafe void WriteBitmap(IRuntimeContext ctx, WriteableBitmap bitmap)
    {
        using var frameBuffer = bitmap.Lock();
        var bytesSize = frameBuffer.RowBytes * frameBuffer.Size.Height;
        var address = frameBuffer.Address;
        Span<byte> span = new(address.ToPointer(), bytesSize);
        ctx.GraphicsModule.RenderStream.Position = 0;
        ctx.GraphicsModule.RenderStream.Read(span);
    }

    private bool ResizeBitmap(int width, int height)
    {
        var size = new PixelSize(width, height);
        var dpi = new Vector(96, 96);
        var pFormat = PixelFormat.Rgba8888;
        var aFormat = AlphaFormat.Opaque;
        if (_bitmap == null || _bitmap.PixelSize != size)
        {
            _prevBitmap = _bitmap;
            _bitmap = new WriteableBitmap(size, dpi, pFormat, aFormat);
            Render.Source = _bitmap;
            return true;
        }
        return false;
    }
}