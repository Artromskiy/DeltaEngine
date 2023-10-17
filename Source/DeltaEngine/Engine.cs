using System;

namespace DeltaEngine
{
    public sealed class Engine : IDisposable
    {
        private readonly Renderer? _renderer;
        private readonly HelloTriangleApplication? tr;
        public Engine()
        {
            tr = new HelloTriangleApplication();
            _renderer = new Renderer("lol");
        }

        public void Run()
        {
            _renderer?.Run();
            tr?.DrawFrame();
            tr?.Update();
        }

        public void Dispose()
        {
            tr?.Dispose();
            _renderer?.Dispose();
        }
    }
}