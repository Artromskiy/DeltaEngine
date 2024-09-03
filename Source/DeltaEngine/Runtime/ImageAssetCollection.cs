using Delta.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;

namespace Delta.Runtime;
internal class ImageAssetCollection : DefaultAssetCollection<ImageData>
{
    private readonly Dictionary<Guid, WeakReference<ImageData?>> _meshDataMap = [];

    protected override ImageData LoadAsset(string path)
    {
        var image = Image.Load(path);
        return new ImageData(image);
    }

    protected override void SaveAsset(ImageData asset, string path)
    {
        asset.image.Save(path, new PngEncoder());
    }
}

internal class ImageData : IAsset
{
    public readonly Image image;

    public ImageData(Image image)
    {
        this.image = image;
    }
}
