using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;
using System.IO;

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
        Program.RuntimeLoader.OnUIThreadLoop += Scene.UpdateScene;
        AssetSearch.OnOpenedChanged += x => Inspector.IsVisible = !x;

        //Program.RuntimeLoader.OnUIThreadLoop += _explorer.UpdateExplorer;

        Hierarchy.OnEntitySelected += Inspector.SetSelectedEntity;
    }

    private void CreateTestScene(object? sender, RoutedEventArgs e)
    {
        Program.RuntimeLoader.OnRuntimeThread += ctx => ctx.SceneManager.CreateTestScene();
    }

    private void OpenProjectFolder(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(Program.ProjectPath.RootDirectory))
                Process.Start("explorer.exe", Program.ProjectPath.RootDirectory);
        }
        catch { }
    }

    private void OpenTempFolder(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(Program.ProjectPath.TempDirectory))
                Process.Start("explorer.exe", Program.ProjectPath.TempDirectory);
        }
        catch { }
    }
}