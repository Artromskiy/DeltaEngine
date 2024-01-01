using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS;
using Delta.Files;
using Delta.Files.Defaults;
using Delta.Rendering;
using Delta.Scenes;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;

namespace Delta;

public sealed partial class Engine : IDisposable
{
    private readonly Renderer _renderer;
    private readonly AssetImporter _assetImporter = new();
    private readonly Scene _scene;
    private readonly World _world;
    private bool firstRun = true;

    /// <summary>
    /// Mobiles seems to feel good with that count of triangles
    /// </summary>
    //private readonly int N = 1_000_000;
    //private readonly int N = 500_000;
    //private readonly int N = 300_000;
    //private readonly int N = 200_000;
    //private readonly int N = 100_000;
    private readonly int N = 10_000;
    //private readonly int N = 1_000;
    //private readonly int N = 100;
    //private readonly int N = 10;
    //private readonly int N = 2;

    public Engine()
    {
        //using ModelImporter mi = new();
        //mi.Import("C:\\Users\\FLOW\\Downloads\\Ships_parts_test (1).fbx");

        _world = InitWorld();
        new JobScheduler.JobScheduler("WorkerThread");
        _renderer = new Renderer(_world, "Delta Engine");
        _scene = new Scene(_world, _renderer);
    }

    private World InitWorld()
    {
        var world = World.Create();
        for (int i = 0; i < N; i++)
            world.Create<Transform>(new Transform() { Position = Vector3.One, Rotation = Quaternion.Identity, Scale = Vector3.One});
        var transforms = ArrayPool<Entity>.Shared.Rent(N);
        world.GetEntities(new QueryDescription().WithAll<Transform>(), transforms);
        for (int i = 0; i < N / 2; i++)
        {
            transforms[i].Add(new ChildOf() { parent = world.Reference(transforms[i + (N / 2)]) });
            transforms[i].Add(new Render() { Material = DeltaMesh.VCMat, Mesh = DeltaMesh.Mesh });
        }
        ArrayPool<Entity>.Shared.Return(transforms);
        world.TrimExcess();
        return world;
    }


    private readonly Stopwatch sw = new();
    private float deltaTime;

    public void Run()
    {
        sw.Restart();
        _scene.Run(deltaTime);
        sw.Stop();
        deltaTime = (float)sw.Elapsed.TotalSeconds;
        if (firstRun)
        {
            _world.TrimExcess();
            //GC.Collect();
            firstRun = false;
        }
    }

    public void Dispose()
    {
        _renderer?.Dispose();
    }
}