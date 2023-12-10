using Arch.Core;
using DeltaEngine.Files;
using DeltaEngine.Rendering;
using DeltaEngine.Scenes;
using System;
using System.Numerics;

namespace DeltaEngine;

public sealed class Engine : IDisposable
{
    private readonly Renderer _renderer;
    private readonly AssetImporter _assetImporter = new();
    private readonly Scene _scene;

    private readonly int N = 100000;

    public Engine()
    {
        using ModelImporter mi = new();
        //mi.Import("C:\\Users\\FLOW\\Downloads\\Ships_parts_test (1).fbx");
        _renderer = new Renderer("Delta Engine");
        _scene = new Scene(InitWorld(), _renderer);
    }

    private World InitWorld()
    {
        var world = World.Create();
        for (int i = 0; i < N; i++)
            world.Create(new Transform() { Scale = Vector3.One, Rotation = Quaternion.Identity });
        return world;
    }

    public void Run()
    {
        _scene.Run();
        //_renderer?.Run();
    }

    public void Draw()
    {
        //_renderer?.SubmitDraw();
    }

    public void Dispose()
    {
        _renderer?.Dispose();
    }
}
