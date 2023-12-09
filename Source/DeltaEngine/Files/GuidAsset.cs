using System;
using System.Runtime.InteropServices;

namespace DeltaEngine.Files;

public readonly struct GuidAsset<T> where T : IAsset
{
    public readonly Guid guid;

    public readonly T Asset => AssetImporter.Instance.GetAsset(this);

    internal GuidAsset(Guid guid, bool runtime = false)
    {
        this.guid = guid;
    }
}