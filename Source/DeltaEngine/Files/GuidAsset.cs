using System;

namespace Delta.Files;

public interface IAsset { }

public readonly struct GuidAsset<T> where T : IAsset
{
    public readonly Guid guid;

    public readonly T Asset => AssetImporter.Instance.GetAsset(this);

    internal GuidAsset(Guid guid)
    {
        this.guid = guid;
    }
}