using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Delta.ECS;
using Delta.Files;
using Delta.Rendering;
using Delta.Scenes;
using System;
using System.Diagnostics;
using System.Numerics;

namespace Delta;

public sealed class Engine : IDisposable
{
    private readonly Renderer _renderer;
    private readonly AssetImporter _assetImporter = new();
    private readonly Scene _scene;

    /// <summary>
    /// Mobiles seems to feel good with that count of triangles
    /// </summary>
    //private readonly int N = 1_000_000;
    //private readonly int N = 500_000;
    //private readonly int N = 300_000;
    //private readonly int N = 200_000;
    //private readonly int N = 100_000;
    //private readonly int N = 10_000;
    //private readonly int N = 1_000;
    //private readonly int N = 100;
    private readonly int N = 10;

    public Engine()
    {
        //using ModelImporter mi = new();
        //mi.Import("C:\\Users\\FLOW\\Downloads\\Ships_parts_test (1).fbx");

        var world = InitWorld();
        new JobScheduler.JobScheduler("WorkerThread");
        _renderer = new Renderer(world, "Delta Engine");
        _scene = new Scene(world, _renderer);
    }

    private World InitWorld()
    {
        var world = World.Create();
        world.Reserve([Component<Transform>.ComponentType, Component<ChildOf>.ComponentType], N);
        for (int i = 0; i < N; i++)
            world.Create(new Transform() { Scale = Vector3.One });
        var transforms = new Entity[N];
        world.GetEntities(new QueryDescription().WithAll<Transform>(), transforms);
        for (int i = 0; i < N / 2; i++)
            transforms[i].Add(new ChildOf() { parent = world.Reference(transforms[i + (N / 2)]) });
        return world;
    }

    private float deltaTime;
    private readonly Stopwatch sw = new();


    public TimeSpan GetUpdateRendererMetric => _renderer.GetUpdateMetric;
    public TimeSpan GetCopyRendererMetric => _renderer.GetCopyMetric;
    public TimeSpan GetCopySetupRendererMetric => _renderer.GetCopySetupMetric;
    public TimeSpan GetSyncRendererMetric => _renderer.GetSyncMetric;
    public TimeSpan GetAcquireFrameRendererMetric => _renderer.GetAcquireMetric();
    public TimeSpan GetRecordDrawRenderMetric => _renderer.GetRecordDrawMetric();
    public TimeSpan GetSubmitDrawRenderMetric => _renderer.GetSubmitDrawMetric();
    public TimeSpan GetSubmitPresentRenderMetric => _renderer.GetSubmitPresentMetric();
    public TimeSpan GetSceneMetric => _scene.GetSceneMetric;
    public double GetRenderSkipPercent => _renderer.SkippedPercent;

    public void ClearRendererMetrics()
    {
        _renderer.ClearCounters();
        _scene.ClearSceneMetric();
    }


    public void Run()
    {
        sw.Restart();
        _scene.Run(deltaTime);
        sw.Stop();
        deltaTime = (float)sw.Elapsed.TotalSeconds;
    }

    public void Dispose()
    {
        _renderer?.Dispose();
    }
}
