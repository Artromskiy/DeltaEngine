using System;

namespace Delta.Runtime;

public interface IRuntimeContext
{
    public IAssetImporter AssetImporter { get; init; }
    public IProjectPath ProjectPath { get; init; }

    private static IRuntimeContext? _current;
    internal static IRuntimeContext Current
    {
        get => _current!;
        set => _current = value ?? throw new InvalidOperationException($"{nameof(IRuntimeContext)} can not be set to null");
    }
}