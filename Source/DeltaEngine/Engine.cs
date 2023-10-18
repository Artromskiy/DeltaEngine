using System;
namespace DeltaEngine;

public sealed class Engine : IDisposable
{
    private readonly Renderer? _renderer;
    public Engine()
    {
        _renderer = new Renderer("lol");
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
}
