using System;

namespace Delta.Runtime;

public interface IRuntimeContext
{
    public bool Running { get; set; }
    public IAssetCollection AssetImporter { get; }
    public IProjectPath ProjectPath { get; }
    public ISceneManager SceneManager { get; }
    public IGraphicsModule GraphicsModule { get; }

    private static IRuntimeContext? _current;
    internal static IRuntimeContext Current
    {
        get => _current!;
        set => _current = value ?? throw new InvalidOperationException($"{nameof(IRuntimeContext)} can not be set to null");
    }
}