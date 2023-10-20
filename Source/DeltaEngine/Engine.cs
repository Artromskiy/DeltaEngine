using System;
using DeltaEngine.Rendering;

namespace DeltaEngine;



public sealed class Engine : IDisposable
{
    private readonly Renderer _renderer;
    public Engine()
    {
        _renderer = new Renderer("Delta Engine");
    }

    public void Run()
    {
        _renderer?.Run();
        _renderer?.Draw();
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
