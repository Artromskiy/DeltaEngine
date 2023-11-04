using DeltaEngine.Files;
using DeltaEngine.Rendering;
using System;

namespace DeltaEngine;

public sealed class Engine : IDisposable
{
    private readonly Renderer _renderer;
    public Engine()
    {
        using ModelImporter mi = new ModelImporter();
        mi.Import("C:\\Users\\FLOW\\Downloads\\Ships_parts_test (1).fbx");
        _renderer = new Renderer("Delta Engine");
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
