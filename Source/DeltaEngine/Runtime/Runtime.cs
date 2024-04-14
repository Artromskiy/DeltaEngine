using Arch.Core;
using Arch.Core.Extensions;
using Delta.Scenes;
using Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Delta.Runtime;

public sealed class Runtime : IRuntime, IDisposable
{
    public IRuntimeContext Context { get; }
    public event Action? RuntimeCall;

    private readonly EventWaitHandle _loopRunning = new(false, EventResetMode.ManualReset);
    private readonly EventWaitHandle _loopEnded = new(true, EventResetMode.ManualReset);
    private bool _disposed = false;
    private bool _firstRun = false;

    private Scene? _scene;

    private readonly Thread _runtimeThread;

    public Runtime(IProjectPath projectPath)
    {
        var path = projectPath;
        var assimp = new AssetImporter(path);
        Context = new DefaultRuntimeContext(path, assimp);

        _runtimeThread = new Thread(Loop);
        _runtimeThread.Name = "RuntimeThread." + _runtimeThread.ManagedThreadId;
        _runtimeThread.Start();

        IRuntimeContext.Current = Context;
    }

    internal bool _running = false;
    public bool Running
    {
        get => _running;
        set
        {
            if (value == _running)
                return;

            if (value)
            {
                _loopEnded.WaitOne();
                _loopRunning.Set();
            }
            else
            {
                _loopRunning.Reset();
                _loopEnded.WaitOne();
            }
            _running = value;
        }
    }

    private void Loop()
    {
        while (!_disposed)
        {
            _loopRunning.WaitOne();
            _loopEnded.Reset();

            if (_disposed)
                break;

            InternalRun();
            _loopEnded.Set();

            RuntimeCall?.Invoke();
        }
    }

    private void InternalRun()
    {
        if (_scene == null)
        {
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


    public void CreateScene()
    {
        using var _ = Pause;
        _scene?.Dispose();
        _scene = new Scene();
    }

    public void CreateTestScene()
    {
        using var _ = Pause;
        _scene?.Dispose();
        _scene = TestScene.Scene;
        // DO NOT DELETE. Somehow we can get "device lost" without prerender
        RunOnce();
    }

    public List<EntityReference> GetEntities()
    {
        if (_scene == null || _scene._world == null)
            return [];

        var archetypes = _scene._world.Archetypes;
        List<EntityReference> references = [];
        foreach (var item in archetypes)
            foreach (var chunk in item)
                references.AddRange(chunk.Entities[..chunk.Size].Select(e => e.Reference()));
        return references;
    }

    public void LoadScene(string path)
    {
        using var _ = Pause;
        _scene?.Dispose();
        _scene = IRuntimeContext.Current.AssetImporter.GetAsset<Scene>(path);
    }

    public void SaveScene(string name)
    {
        using var _ = Pause;
        IRuntimeContext.Current.AssetImporter.CreateAsset<Scene>(name, _scene);
    }

    public void RunOnce()
    {
        using var run = Run;
        using var pause = Pause;
    }

    public Scene? GetCurrentScene() => _scene;

    public void Dispose()
    {
        using var _ = Pause;
        RuntimeCall = null;
        World.SharedJobScheduler?.Dispose();
        World.SharedJobScheduler = null;
        _scene?.Dispose();
        _disposed = true;
    }


    public PauseHandle Pause => new(this, true);
    public PauseHandle Run => new(this, false);
}