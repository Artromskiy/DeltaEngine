using Delta.Engine.Rendering;
using Delta.Engine.Runtime;
public static class RuntimeContextFactory
{
    public static IRuntimeContext CreateHeadlessContext(IProjectPath projectPath)
    {
        var path = projectPath;
        var assets = new GlobalAssetCollection();
        var sceneManager = new SceneManager();
        var graphics = new NullGraphicsModule("Delta Editor");

        return new DefaultRuntimeContext(path, assets, sceneManager, graphics);
    }

    public static IRuntimeContext CreateWindowedContext(IProjectPath projectPath)
    {
        var path = projectPath;
        var assets = new GlobalAssetCollection();
        var sceneManager = new SceneManager();
        var graphics = new NullGraphicsModule("Delta Editor");

        return new DefaultRuntimeContext(path, assets, sceneManager, graphics);
    }
}
