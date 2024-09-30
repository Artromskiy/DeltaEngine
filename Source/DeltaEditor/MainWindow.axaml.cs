using Avalonia.Controls;
using Avalonia.Interactivity;
using Delta.Runtime;
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
        new FlyoutSearchControl();
        Program.RuntimeLoader.OnLoop += Inspector.UpdateInspector;
        Program.RuntimeLoader.OnLoop += Hierarchy.UpdateHierarchy;
        Program.RuntimeLoader.OnLoop += Scene.UpdateScene;

        Hierarchy.OnEntitySelected += Inspector.SetSelectedEntity;
        Program.RuntimeLoader.Init();
    }

    private void CreateTestScene(object? sender, RoutedEventArgs e) { }

    private void OpenProjectFolder(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(IRuntimeContext.Current.ProjectPath.RootDirectory))
                Process.Start("explorer.exe", IRuntimeContext.Current.ProjectPath.RootDirectory);
        }
        catch { }
    }

    private void OpenTempFolder(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(IRuntimeContext.Current.ProjectPath.TempDirectory))
                Process.Start("explorer.exe", IRuntimeContext.Current.ProjectPath.TempDirectory);
        }
        catch { }
    }
}