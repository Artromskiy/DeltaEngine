using Arch.Core;
using Delta.Assets;
using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Delta.Runtime;

public sealed class Scene : IDisposable, IAsset
{
    internal readonly World _world;
    [JsonIgnore]
    private readonly List<ISystem> _jobs;

    private readonly List<ISystem> _defaultJobs;

    [JsonIgnore]
    private readonly HierarchySystem _hierarchySystem = new();
    private readonly Stopwatch _deltaTimeSw = new();
    private float _deltaTime;

    public Scene()
    {
        _world = World.Create();
        _jobs = [];
        _defaultJobs = [_hierarchySystem.MarkDestroySystem()];
    }

    public float DeltaTime() => _deltaTime;

    public void Run(float deltaTime)
    {
        if (IRuntimeContext.Current.Running)
        {
            _deltaTime = deltaTime;
            foreach (var item in _jobs)
                item.Execute();
        }
        foreach (var item in _defaultJobs)
            item.Execute();
    }

    public void Run()
    {
        var deltaTime = (float)_deltaTimeSw.Elapsed.TotalSeconds;
        _deltaTimeSw.Restart();
        Run(deltaTime);
    }

    [Imp(Sync)]
    public void AddSystem(ISystem job) => _jobs.Add(job);
    [Imp(Sync)]
    public void RemoveSystem(ISystem job) => _jobs.Remove(job);
    [Imp(Sync)]
    public void RemoveEntity(EntityReference entityRef)
    {
        IRuntimeContext.Current.SceneManager.CurrentScene._world.AddOrGet<DestroyFlag>(entityRef);
    }

    [Imp(Sync)]
    public EntityReference AddEntity()
    {
        var entityRef = _world.Reference(_world.Create());
        _hierarchySystem.AddRootEntity(entityRef);
        return entityRef;
    }

    public EntityReference[] GetRootEntities()
    {
        return _hierarchySystem.GetRootEntities();
    }
    public void GetFirstChildren(EntityReference entityRef, List<EntityReference> children)
    {
        _hierarchySystem.GetFirstChildren(entityRef, children);
    }
    public int GetFirstChildrenCount(EntityReference entityRef)
    {
        return _hierarchySystem.GetFirstChildrenCount(entityRef);
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