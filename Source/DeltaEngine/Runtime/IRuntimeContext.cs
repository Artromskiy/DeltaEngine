using System;

namespace Delta.Runtime;

public interface IRuntimeContext : IDisposable
{
    internal IRuntimeContext? PreviousContext { get; set; }
    public bool Running { get; set; }
    public IAssetCollection AssetImporter { get; }
    public IProjectPath ProjectPath { get; }
    public ISceneManager SceneManager { get; }
    public IGraphicsModule GraphicsModule { get; }

    private static IRuntimeContext? _current;
    public static IRuntimeContext Current
    {
        get => _current!;
        set
        {
            value.PreviousContext = _current;
            _current = value ??
                throw new InvalidOperationException($"{nameof(IRuntimeContext)} can not be set to null");
        }
    }
    void IDisposable.Dispose()
    {
        _current = PreviousContext ?? _current;
        PreviousContext = null;
    }
}