using Arch.Core;
using Arch.Core.Extensions;
using Delta.Files.Defaults;
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

    private void Loop()
    {
        while (!_disposed)
        {
            if (_disposed)
                break;

            RunScene();
            RuntimeCall?.Invoke();
        }

        World.SharedJobScheduler?.Dispose();
        World.SharedJobScheduler = null;
        _scene?.Dispose();
    }

    private void RunScene()
    {
        if (_scene == null)
            return;

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
        _scene?.Dispose();
        _scene = new Scene();
    }

    public void CreateTestScene()
    {
        _scene?.Dispose();
        _scene = TestScene.Scene;
        // DO NOT DELETE. Somehow we can get "device lost" without prerender
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
        _scene?.Dispose();
        _scene = IRuntimeContext.Current.AssetImporter.GetAsset<Scene>(path);
    }

    public void SaveScene(string name)
    {
        IRuntimeContext.Current.AssetImporter.CreateAsset<Scene>(name, _scene);
    }


    public Scene? GetCurrentScene() => _scene;

    public void Dispose()
    {
        RuntimeCall = null;
        _disposed = true;
    }
}