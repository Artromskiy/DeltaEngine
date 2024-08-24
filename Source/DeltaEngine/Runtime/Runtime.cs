using Arch.Core;
using Delta.Rendering;
using Schedulers;
using System;
using System.Threading;

namespace Delta.Runtime;

public sealed class Runtime : IRuntime, IDisposable
{
    public IRuntimeContext Context { get; }
    public event Action? RuntimeCall;
    public event Action? RuntimeLoopEnd;

    private bool _disposed = false;

    private readonly Thread _runtimeThread;
    private JobScheduler.Config _jobConfig = new()
    {
        ThreadPrefixName = "Arch.Multithreading",
        ThreadCount = 0,
        MaxExpectedConcurrentJobs = 64,
        StrictAllocationMode = false,
    };

    public Runtime(IProjectPath projectPath)
    {
        var path = projectPath;
        var assets = new AssetCollection(path);
        var sceneManager = new SceneManager();
        var graphics = new GraphicsModule("Delta Editor"); // new DummyGraphics();

        Context = new DefaultRuntimeContext(path, assets, sceneManager, graphics);

        _runtimeThread = new Thread(Loop);
        _runtimeThread.Name = "RuntimeThread." + _runtimeThread.ManagedThreadId;
        _runtimeThread.Start();

        IRuntimeContext.Current = Context;
    }

    private void Loop()
    {
        World.SharedJobScheduler ??= new JobScheduler(_jobConfig);
        while (!_disposed)
        {
            if (_disposed)
                break;

            IRuntimeContext.Current.SceneManager.Execute();
            RuntimeCall?.Invoke();
            IRuntimeContext.Current.GraphicsModule.Execute();
            RuntimeLoopEnd?.Invoke();
        }

        World.SharedJobScheduler?.Dispose();
        World.SharedJobScheduler = null;
    }

    public void Dispose()
    {
        RuntimeCall = null;
        _disposed = true;
    }
}