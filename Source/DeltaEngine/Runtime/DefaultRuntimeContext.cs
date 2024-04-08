namespace Delta.Runtime;

internal record DefaultRuntimeContext(IProjectPath ProjectPath, IAssetImporter AssetImporter) : IRuntimeContext;
