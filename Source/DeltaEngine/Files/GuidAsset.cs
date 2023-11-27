using System;

namespace DeltaEngine.Files;
public readonly struct GuidAsset<T>
{
    public readonly Guid guid;
    private readonly object? _runtimeRef;
    public readonly T Asset => AssetImporter.Instance.GetAsset(this);

    public GuidAsset(Guid guid, bool runtime = false)
    {
        this.guid = guid;
        _runtimeRef = runtime ? new() : null;
    }
}
