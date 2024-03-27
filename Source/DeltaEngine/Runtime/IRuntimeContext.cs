using System;

namespace Delta.Runtime;

public interface IRuntimeContext
{
    public IAssetImporter AssetImporter { get; }
    public IProjectPath ProjectPath { get; }

    private static IRuntimeContext? _current;
    internal static IRuntimeContext Current
    {
        get => _current!;
        set => _current = value ?? throw new InvalidOperationException($"{nameof(IRuntimeContext)} can not be set to null");
    }
}