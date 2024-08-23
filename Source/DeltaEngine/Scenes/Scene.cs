using Arch.Core;
using Delta.ECS;
using Delta.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Delta.Scenes;

public sealed class Scene : IDisposable, IAsset
{
    internal readonly World _world;
    [JsonIgnore]
    private readonly List<ISystem> _jobs;

    private float _deltaTime;
    private readonly Stopwatch _sceneSw = new();

    public Scene()
    {
        _world = World.Create();
        _jobs = [];
    }

    public void Run()
    {
        _sceneSw.Restart();

        foreach (var item in _jobs)
            item.Execute();

        _sceneSw.Stop();
        _deltaTime = (float)_sceneSw.Elapsed.TotalSeconds;
    }

    public float DeltaTime() => _deltaTime;

    public void AddJob(ISystem job)
    {
        _jobs.Add(job);
    }

    public void RemoveJob(ISystem job)
    {
        _jobs.Remove(job);
    }

    public void AddEntity()
    {
        _world.Create();
    }

    public void RemoveEntity()
    {
        var all = QueryDescription.Null;
        Span<Entity> e = stackalloc Entity[_world.CountEntities(all)];
        _world.GetEntities(all, e);
        _world.Destroy(e[0]);
    }

    public void Dispose()
    {
        World.Destroy(_world);
        foreach (var item in _jobs)
            if (item is IDisposable disposable)
                disposable.Dispose();
        _jobs.Clear();
    }
}