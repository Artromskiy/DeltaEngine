using Avalonia.Controls;
using Avalonia.Interactivity;
using DeltaEditor;

namespace DeltaEditor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
            return;

        //Program.RuntimeLoader

        Program.RuntimeLoader.OnUIThreadLoop += Inspector.UpdateInspector;
        Program.RuntimeLoader.OnUIThreadLoop += Hierarchy.UpdateHierarchy;
        //Program.RuntimeLoader.OnUIThreadLoop += _explorer.UpdateExplorer;

        Hierarchy.OnEntitySelected += Inspector.SetSelectedEntity;
        CreateSceneButton.Click += Button_Click;
    }

    public void Button_Click(object? sender, RoutedEventArgs e)
    {
        Program.RuntimeLoader.OnRuntimeThread += ctx => ctx.SceneManager.CreateTestScene();
    }
}