using Silk.NET.Windowing;

namespace Delta.Runtime;
public static class RuntimeContextFactory
{
    public static IRuntimeContext CreateHeadlessContext(IProjectPath projectPath)
    {
        var path = projectPath;
        var assets = new GlobalAssetCollection();
        var sceneManager = new SceneManager();
        var graphics = new Rendering.Headless.HeadlessGraphicsModule("Delta Editor");

        return new DefaultRuntimeContext(path, assets, sceneManager, graphics);
    }

    public static IRuntimeContext CreateWindowedContext(IProjectPath projectPath)
    {
        var path = projectPath;
        var assets = new GlobalAssetCollection();
        var sceneManager = new SceneManager();
        var graphics = new Rendering.SdlRendering.WindowedGraphicsModule("Delta Editor");

        return new DefaultRuntimeContext(path, assets, sceneManager, graphics);
    }
}
