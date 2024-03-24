using Arch.Core;
using Delta.Files;
using JobScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Delta.Scenes;

public sealed class Scene : IDisposable, IAsset
{
    internal readonly World _world;
    [JsonIgnore]
    private readonly List<IJob> _jobs;

    private float _deltaTime;
    private readonly Stopwatch _sceneSw = new();

    private const bool WriteMetrics = false;

    private readonly Dictionary<Type, TimeSpan> _metrics = [];
    public void ClearMetrics() => _metrics.Clear();
    public Dictionary<string, TimeSpan> GetMetrics()
    {
        Dictionary<string, TimeSpan> result = [];
        foreach (var item in _metrics)
            result.Add(item.Key.ToString(), item.Value);
        return result;
    }

    public Scene()
    {
        _world = World.Create();
        _jobs = [];
    }

    private readonly Stopwatch _jobWatch = new();

    public void Run()
    {
        _sceneSw.Restart();

        foreach (var item in _jobs)
        {
            _jobWatch.Restart();
            item.Execute();
            _jobWatch.Stop();

            if (WriteMetrics)
            {
                var type = item.GetType();
                _metrics.TryAdd(type, TimeSpan.Zero);
                _metrics[type] += _jobWatch.Elapsed;
            }
        }

        _sceneSw.Stop();
        _deltaTime = (float)_sceneSw.Elapsed.TotalSeconds;
    }

    public float DeltaTime() => _deltaTime;

    public void AddJob(IJob job)
    {
        _jobs.Add(job);
    }

    public void RemoveJob(IJob job)
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
            using (item as IDisposable) ;
        _jobs.Clear();
    }
}