using DeltaEngine;

namespace DeltaEditor
{
    public partial class App : Application
    {
        private readonly Engine _engine;
        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
            _engine = new Engine();
            _engine.Run();
        }
    }
}