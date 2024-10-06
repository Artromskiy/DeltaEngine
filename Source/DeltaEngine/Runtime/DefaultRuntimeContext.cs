namespace Delta.Runtime;

internal record DefaultRuntimeContext(
    IProjectPath ProjectPath,
    IAssetCollection AssetImporter,
    ISceneManager SceneManager,
    IGraphicsModule GraphicsModule)
    : IRuntimeContext
{
    public bool Running { get; set; }
    public IRuntimeContext? PreviousContext { get; set; }
}

