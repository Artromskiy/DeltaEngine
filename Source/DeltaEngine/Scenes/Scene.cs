using Arch.Core;
//using Arch.Core.Extensions;
using Delta.ECS;
using Delta.ECS.Components.Hierarchy;
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

    [JsonIgnore]
    private readonly HierarchySystem _hierarchySystem = new();

    private float _deltaTime;

    public Scene()
    {
        _world = World.Create();
        _jobs = [];
    }
    public float DeltaTime() => _deltaTime;

    public void Run(float deltaTime)
    {
        _hierarchySystem.UpdateOrders();
        _deltaTime = deltaTime;
        foreach (var item in _jobs)
            item.Execute();
    }

    public void AddJob(ISystem job)
    {
        _jobs.Add(job);
    }

    public void RemoveJob(ISystem job)
    {
        _jobs.Remove(job);
    }

    public void RemoveEntity(EntityReference entityRef)
    {
        _hierarchySystem.RemoveEntity(entityRef);
        _world.Destroy(entityRef);
    }

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

    public void Dispose()
    {
        World.Destroy(_world);
        foreach (var item in _jobs)
            if (item is IDisposable disposable)
                disposable.Dispose();
        _jobs.Clear();
    }
}