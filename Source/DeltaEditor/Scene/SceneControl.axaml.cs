using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Delta.Runtime;
using Delta.Utilities;

namespace DeltaEditor;

public partial class SceneControl : UserControl
{
    private WriteableBitmap? _bitmap;
    private WriteableBitmap? _prevBitmap;
    private readonly UnmanagedMemoryManager<byte> _bitmapMemoryManager = new(0, 0);
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
            WriteBitmap(ctx, _bitmap!, _bitmapMemoryManager);
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

    private static unsafe void WriteBitmap(IRuntimeContext ctx, WriteableBitmap bitmap, UnmanagedMemoryManager<byte> bitmapMemoryManager)
    {
        using var frameBuffer = bitmap.Lock();
        var renderStream = ctx.GraphicsModule.RenderStream;
        bitmapMemoryManager.UpdateSource(frameBuffer.Address, frameBuffer.RowBytes * frameBuffer.Size.Height);
        ctx.GraphicsModule.RenderStream.CopyToParallel(bitmapMemoryManager.Memory);
    }

    private bool ResizeBitmap(int width, int height)
    {
        var size = new PixelSize(width, height);
        var dpi = new Vector(96, 96);
        var pFormat = PixelFormat.Rgb32;
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