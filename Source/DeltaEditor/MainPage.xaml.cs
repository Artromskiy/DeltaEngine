using Delta;

namespace DeltaEditor
{
    public partial class MainPage : ContentPage
    {
        private readonly Engine _engine;

        public MainPage(Engine engine)
        {
            _engine = engine;
            InitializeComponent();
        }

        private void CreateFile(object sender, EventArgs e)
        {
            _engine.CreateFile();
        }

        private void CreateScene(object sender, EventArgs e)
        {
            _engine.CreateScene();
        }

        private void OnClickRun(object sender, EventArgs e)
        {
            _engine.GetCurrentScene().Run();
        }

        private void AddEntity(object sender, EventArgs e)
        {
            _engine.GetCurrentScene().AddEntity();
        }
        private void RemoveEntity(object sender, EventArgs e)
        {
            _engine.GetCurrentScene().RemoveEntity();
        }
    }
}
