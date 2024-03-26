namespace Delta.Runtime;

internal record RuntimeContext(IProjectPath ProjectPath, IAssetImporter AssetImporter) : IRuntimeContext;
