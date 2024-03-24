using Delta.Files;
using Delta.Scenes;
using System;
using System.Threading;

namespace Delta.Runtime;

public class Runtime : IRuntime, IDisposable
{
    public IProjectPath ProjectPath { get; }
    public IAssetImporter AssetImporter { get; }
    private readonly SceneCollection? _sceneCollection;
    private readonly JobScheduler.JobScheduler _jobScheduler = new("WorkerThread");

    private bool _disposed = false;

    private bool _firstRun = false;

    private Scene? _scene;

    private readonly Thread _runtimeThread;

    private readonly PauseHandle _pauseHandle = new();


    public bool Running
    {
        get => _pauseHandle.Running;
        set => _pauseHandle.Running = value;
    }

    public Runtime(IProjectPath projectPath)
    {
        ProjectPath = projectPath;
        AssetImporter = new AssetImporter(ProjectPath);
        _sceneCollection = new(ProjectPath);
        _runtimeThread = new Thread(Loop);
        _runtimeThread.Start();
    }

    public void CreateScene()
    {
        using var _ = _pauseHandle.Pause();
        _scene?.Dispose();
        _scene = new Scene();
    }

    public void CreateTestScene()
    {
        using var _ = _pauseHandle.Pause();
        _scene?.Dispose();
        _scene = TestScene.Scene;
    }

    public void SaveScene(string name)
    {
        using var _ = _pauseHandle.Pause();
        AssetImporter.CreateAsset<Scene>(name, _scene);
    }

    public void RunOnce()
    {
        using var _ = _pauseHandle.Pause();
        InternalRun();
    }

    private void InternalRun()
    {
        if (_scene == null)
            throw new InvalidOperationException("No loaded scenes to run");

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
                return;

            InternalRun();
            Thread.Yield();
            _pauseHandle.loopEnded.Set();
        }
    }

    public Scene? GetCurrentScene() => _scene;
    public void Dispose()
    {
        using var _ = _pauseHandle.Pause();

        _scene?.Dispose();
        _disposed = true;
    }

    private class PauseHandle : IDisposable
    {
        public readonly EventWaitHandle loopStart = new(false, EventResetMode.ManualReset);
        public readonly EventWaitHandle loopEnded = new(true, EventResetMode.ManualReset);
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

        private bool _running = false;
        private bool state = false;

        public PauseHandle Pause()
        {
            state = _running;
            Running = false;
            return this;
        }

        public void Dispose() => Running = state;
    }
}