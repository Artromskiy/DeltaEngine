using System;
using System.Diagnostics.CodeAnalysis;

namespace DeltaEngine.Files;

public readonly struct GuidAsset<T> where T: IAsset
{
    public readonly Guid guid;
    internal readonly object? _runtimeRef;

    public readonly T Asset => AssetImporter.Instance.GetAsset(this);

    public GuidAsset(Guid guid, bool runtime = false)
    {
        this.guid = guid;
        _runtimeRef = runtime ? new() : null;
    }
}