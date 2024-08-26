using Arch.Core;
using Delta.ECS;
using Delta.Files;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Delta.Scenes;

public sealed class Scene : IDisposable, IAsset
{
    internal readonly World _world;
    [JsonIgnore]
    private readonly List<ISystem> _jobs;

    private float _deltaTime;

    public Scene()
    {
        _world = World.Create();
        _jobs = [];
    }

    public void Run(float deltaTime)
    {
        _deltaTime = deltaTime;
        foreach (var item in _jobs)
            item.Execute();
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

    public void RemoveEntity(Entity entity)
    {
        _world.Destroy(entity);
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