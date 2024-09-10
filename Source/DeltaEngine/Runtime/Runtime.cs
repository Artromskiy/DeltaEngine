using Arch.Core;
using Schedulers;
using System;
using System.Diagnostics;
using System.Threading;

namespace Delta.Runtime;

public sealed class Runtime : IRuntime, IDisposable
{
    public IRuntimeContext Context { get; }
    public event Action? RuntimeCall;

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
        var assets = new GlobalAssetCollection(path);
        var sceneManager = new SceneManager();
        var graphics = new Rendering.Headless.HeadlessGraphicsModule("Delta Editor");
        //var graphics = new Rendering.SdlRendering.SdlGraphicsModule("Delta Editor");
        //var graphics = new DummyGraphics();

        Context = new DefaultRuntimeContext(path, assets, sceneManager, graphics);

        _runtimeThread = new Thread(Loop);
        _runtimeThread.Name = "RuntimeThread." + _runtimeThread.ManagedThreadId;
        _runtimeThread.Start();

        IRuntimeContext.Current = Context;
    }

    private void Loop()
    {
        World.SharedJobScheduler ??= new JobScheduler(_jobConfig);
        Stopwatch sw = new();
        float deltaTime = 0;
        while (!_disposed)
        {
            if (_disposed)
                break;
            sw.Restart();

            try
            {
                IRuntimeContext.Current.SceneManager.Execute(deltaTime);
                RuntimeCall?.Invoke();
                IRuntimeContext.Current.GraphicsModule.Execute();
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
            sw.Stop();
            deltaTime = (float)sw.Elapsed.TotalSeconds;
            var us = (int)sw.Elapsed.TotalMicroseconds;
            Debug.WriteLine($"{us} us");
            Debug.WriteLine($"{1f / deltaTime} fps");
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