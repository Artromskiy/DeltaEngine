using Delta;
using DeltaEditorLib.Project;
using DeltaEditorLib.Scripting;

namespace DeltaEditor
{
    public partial class MainPage : ContentPage
    {
        private readonly Engine _engine;
        private readonly ProjectPath _projectData;

        public MainPage(Engine engine, ProjectPath projectData)
        {
            _engine = engine;
            _projectData = projectData;
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

        private void TryCompile(object sender, EventArgs e)
        {
            CodeLoader.TryCompile(_projectData.path, _projectData.path);
        }
    }
}
