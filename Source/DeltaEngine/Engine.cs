using Arch.Core;
using DeltaEngine.Files;
using DeltaEngine.Rendering;
using DeltaEngine.Scenes;
using System;
using System.Diagnostics;
using System.Numerics;

namespace DeltaEngine;

public sealed class Engine : IDisposable
{
    private readonly Renderer _renderer;
    private readonly AssetImporter _assetImporter = new();
    private readonly Scene _scene;

    /// <summary>
    /// Mobiles seems to feel good with that count of triangles
    /// </summary>
    private readonly int N = 1_000_000;
    //private readonly int N = 100_000;
    //private readonly int N = 10_000;
    //private readonly int N = 1_000;
    //private readonly int N = 10;

    public Engine()
    {
        //using ModelImporter mi = new();
        //mi.Import("C:\\Users\\FLOW\\Downloads\\Ships_parts_test (1).fbx");

        var world = InitWorld();
        _renderer = new Renderer(world, "Delta Engine");
        _scene = new Scene(world, _renderer);
    }

    private World InitWorld()
    {
        var world = World.Create();
        for (int i = 0; i < N; i++)
            world.Create(new Transform() { Scale = Vector3.One, Rotation = Quaternion.Identity });
        return world;
    }

    private float deltaTime;
    private readonly Stopwatch sw = new();


    public TimeSpan GetUpdateRendererMetric => _renderer.GetUpdateMetric;
    public TimeSpan GetCopyRendererMetric => _renderer.GetCopyMetric;
    public TimeSpan GetCopySetupRendererMetric => _renderer.GetCopySetupMetric;
    public TimeSpan GetSyncRendererMetric => _renderer.GetSyncMetric;
    public TimeSpan GetAcquireFrameRendererMetric => _renderer.GetAcquireMetric();
    public TimeSpan GetSceneMetric => _scene.GetSceneMetric;

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
