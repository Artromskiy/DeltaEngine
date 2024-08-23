namespace Delta.Runtime;

internal record DefaultRuntimeContext(IProjectPath ProjectPath, IAssetCollection AssetImporter, ISceneManager SceneManager, IGraphicsModule GraphicsModule) : IRuntimeContext;
