using DeltaEngine.Files;
using DeltaEngine.Rendering;
using System;
using DeltaEngine.Scenes;
using Arch.Core;

namespace DeltaEngine;

public sealed class Engine : IDisposable
{
    private readonly Renderer _renderer;
    private readonly AssetImporter _assetImporter = new();
    private readonly Scene _scene;
    public Engine()
    {
        using ModelImporter mi = new();
        //mi.Import("C:\\Users\\FLOW\\Downloads\\Ships_parts_test (1).fbx");
        _renderer = new Renderer("Delta Engine");
        _scene = new Scene(InitWorld());
    }

    private World InitWorld()
    {
        var w = World.Create();
        return w;
    }



    public void Run()
    {
        _renderer?.Run();
    }

    public void Draw()
    {
        _renderer?.SubmitDraw();
    }

    public void Dispose()
    {
        _renderer?.Dispose();
    }

    public void SetWindowPositionAndSize((int x, int y, int w, int h) rect)
    {
        _renderer.SetWindowPositionAndSize(rect);
    }
}
