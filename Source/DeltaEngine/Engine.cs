namespace DeltaEngine
{
    public class Engine
    {
        private readonly Renderer _renderer;
        public Engine()
        {
            _renderer = new Renderer();
        }

        public void Run()
        {
            _renderer.Run();
        }
    }
}