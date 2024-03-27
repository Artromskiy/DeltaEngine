using Arch.Core;
using Delta.Scenes;
using Schedulers;
using System;
using System.Threading;

namespace Delta.Runtime;

public class Runtime : IRuntime, IDisposable
{
    public IRuntimeContext Context { get; }

    private bool _disposed = false;
    private bool _firstRun = false;

    private Scene? _scene;

    private readonly Thread _runtimeThread;

    private readonly RuntimeRunner _pauseHandle = new();


    public bool Running
    {
        get => _pauseHandle.Running;
        set => _pauseHandle.Running = value;
    }

    public Runtime(IProjectPath projectPath)
    {
        var path = projectPath;
        var assimp = new AssetImporter(path);
        Context = new DefaultRuntimeContext(path, assimp);

        _runtimeThread = new Thread(Loop);
        _runtimeThread.Start();

        IRuntimeContext.Current = Context;
    }

    public void CreateScene()
    {
        using var _ = _pauseHandle.Pause;
        _scene?.Dispose();
        _scene = new Scene();
    }

    public void CreateTestScene()
    {
        using var _ = _pauseHandle.Pause;
        _scene?.Dispose();
        _scene = TestScene.Scene;
        // DO NOT DELETE. Somehow we can get "device lost" without prerender
        RunOnce();
    }

    public void LoadScene(string path)
    {
        using var _ = _pauseHandle.Pause;
        _scene?.Dispose();
        _scene = IRuntimeContext.Current.AssetImporter.GetAsset<Scene>(path);
    }

    public void SaveScene(string name)
    {
        using var _ = _pauseHandle.Pause;
        IRuntimeContext.Current.AssetImporter.CreateAsset<Scene>(name, _scene);
    }

    public void RunOnce()
    {
        using var run = _pauseHandle.Run;
        using var pause = _pauseHandle.Pause;
    }

    private void InternalRun()
    {
        if (_scene == null)
        {
            Console.WriteLine("Fucked up");
            throw new InvalidOperationException("No loaded scenes to run");
        }
        World.SharedJobScheduler ??= new JobScheduler(new JobScheduler.Config()
        {
            ThreadPrefixName = "Arch.Multithreading",
            ThreadCount = 0,
            MaxExpectedConcurrentJobs = 64,
            StrictAllocationMode = false,
        });

        _scene.Run();
        if (_firstRun)
        {
            _firstRun = false;
            _scene._world.TrimExcess();
            GC.Collect();
        }
    }

    private void Loop()
    {
        while (!_disposed)
        {
            _pauseHandle.loopStart.WaitOne();
            _pauseHandle.loopEnded.Reset();

            if (_disposed)
                break;

            InternalRun();
            //Thread.Yield();
            _pauseHandle.loopEnded.Set();
        }

    }

    public Scene? GetCurrentScene() => _scene;

    public void Dispose()
    {
        using var _ = _pauseHandle.Pause;

        _scene?.Dispose();
        World.SharedJobScheduler?.Dispose();
        World.SharedJobScheduler = null;
        _disposed = true;
    }

    private class RuntimeRunner
    {
        public readonly EventWaitHandle loopStart = new(false, EventResetMode.ManualReset);
        public readonly EventWaitHandle loopEnded = new(true, EventResetMode.ManualReset);

        private bool _running = false;
        public bool Running
        {
            get => _running;
            set
            {
                if (value == _running)
                    return;

                if (value)
                {
                    loopEnded.WaitOne();
                    loopStart.Set();
                }
                else
                {
                    loopStart.Reset();
                    loopEnded.WaitOne();
                }
                _running = value;
            }
        }

        public readonly ref struct PauseHandle
        {
            private readonly bool state;
            private readonly RuntimeRunner _runner;
            public PauseHandle(RuntimeRunner runner, bool value)
            {
                _runner = runner;
                state = _runner._running;
                _runner.Running = !value;
            }

            public void Dispose()
            {
                _runner.Running = state;
            }
        }

        public PauseHandle Pause => new(this, true);
        public PauseHandle Run => new(this, false);
    }
}