using Delta.ECS;
using Delta.Rendering;
using System;
using System.Diagnostics;

namespace Delta.Runtime;

public sealed class Runtime : IRuntime, IDisposable
{
    public IRuntimeContext Context { get; }

    private bool _disposed = false;

    public Runtime(IProjectPath projectPath)
    {
        var path = projectPath;
        var assets = new GlobalAssetCollection();
        var sceneManager = new SceneManager();
        var graphics = new Rendering.Headless.HeadlessGraphicsModule("Delta Editor");

        Context = new DefaultRuntimeContext(path, assets, sceneManager, graphics);
        IRuntimeContext.Current = Context;

        graphics.AddRenderBatcher(new SceneBatcher());
    }

    public void Run()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        JobScheduler.JobScheduler.Instance ??= new JobScheduler.JobScheduler("Arch.Multithreading");
        try
        {
            IRuntimeContext.Current.SceneManager.CurrentScene.Run();
            IRuntimeContext.Current.GraphicsModule.Execute();
            DestroySystem.Execute();
            DirtyFlagClearSystem.Execute();
        }
        catch (Exception e)
        {
            Debug.Assert(false, e.Message);
        }
    }

    public void Dispose()
    {
        JobScheduler.JobScheduler.Instance.Dispose();
        JobScheduler.JobScheduler.Instance = null!;
        _disposed = true;
    }
}