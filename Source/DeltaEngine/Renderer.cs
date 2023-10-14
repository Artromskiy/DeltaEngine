using Silk.NET.Windowing;

namespace DeltaEngine
{
    internal class Renderer : IDisposable
    {
        private readonly IView _view;
        private ViewOptions _options = ViewOptions.DefaultVulkan;

        public Renderer()
        {
            //_options.ShouldSwapAutomatically = true;
            _view = Window.GetView(_options);
            _view.Load += OnLoad;
            _view.Render += OnRender;
            _view.Closing += OnClose;
        }

        public void Run()
        {
            _view.Run();
        }

        private void OnLoad()
        {

        }
        private void OnRender(double d)
        {

        }
        private void OnClose()
        {

        }

        public void Dispose()
        {
            _view.Close();
            _view.Dispose();
        }
    }
}
