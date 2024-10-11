using Arch.Core;
using Delta.ECS;
using Delta.Rendering;
using System;
using System.Diagnostics;
using Schedulers;

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
    public Runtime(IRuntimeContext context)
    {
        Context = context;
        IRuntimeContext.Current = Context;
        IRuntimeContext.Current.GraphicsModule.AddRenderBatcher(new SceneBatcher());
        IRuntimeContext.Current.GraphicsModule.AddRenderBatcher(new UISceneBatcher());
    }

    public void Run()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        World.SharedJobScheduler ??= new JobScheduler(new JobScheduler.Config()
        {
            ThreadPrefixName = "Arch.Multithreading",
        });
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
        World.SharedJobScheduler?.Dispose();
        World.SharedJobScheduler = null!;
        _disposed = true;
    }
}