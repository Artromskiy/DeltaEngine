using Delta.Runtime;
using DeltaEditorLib.Scripting;
using System.Diagnostics;

namespace DeltaEditor
{
    public partial class MainPage : ContentPage
    {
        private readonly IProjectPath _projectData;
        private readonly RuntimeLoader _runtimeLoader;
        private IRuntime Runtime => _runtimeLoader.Runtime;

        public MainPage(RuntimeLoader runtimeLoader, IProjectPath projectData)
        {
            _runtimeLoader = runtimeLoader;
            _projectData = projectData;
            InitializeComponent();
            CompPicker.ItemsSource = runtimeLoader.GetComponentsNames();
        }

        private void CreateScene(object sender, EventArgs e)
        {
            Runtime.CreateTestScene();
        }

        private void RunScene(object sender, ToggledEventArgs e)
        {
            Runtime.Running = e.Value;
        }

        private void SaveScene(object sender, EventArgs e)
        {
            Runtime.SaveScene("scene");
        }

        private void TryCompile(object sender, EventArgs e)
        {
            _runtimeLoader.ReloadRuntime();
            CompPicker.ItemsSource = _runtimeLoader.GetComponentsNames();
        }

        private void UpdateData()
        {
            HierarchyStack.Children.Clear();
            InspectorStack.Children.Clear();
            //_runtimeLoader.Runtime.
        }

        private void OpenProjectFolder(object sender, EventArgs e)
        {
            if (Directory.Exists(_projectData.RootDirectory))
                Process.Start("explorer.exe", _projectData.RootDirectory);
        }

        private void OnPickerComponentIndexChanged(object sender, EventArgs e)
        {
            var picker = (Picker)sender;
            int selectedIndex = picker.SelectedIndex;

            /*
            if (selectedIndex != -1)
            {
                monkeyNameLabel.Text = (string)picker.ItemsSource[selectedIndex];
            }
            */
        }

        private void PlayButton_Clicked(object sender, EventArgs e)
        {

        }

        private void PauseButton_Clicked(object sender, EventArgs e)
        {

        }

        private void NextButton_Clicked(object sender, EventArgs e)
        {

        }
    }
}
