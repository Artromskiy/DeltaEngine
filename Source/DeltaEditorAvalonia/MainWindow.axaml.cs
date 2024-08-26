using Avalonia.Controls;

namespace DeltaEditorAvalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        //Program.RuntimeLoader

        Program.RuntimeLoader.OnUIThreadLoop += Inspector.UpdateInspector;
        Program.RuntimeLoader.OnUIThreadLoop += Hierarchy.UpdateHierarchy;
        //Program.RuntimeLoader.OnUIThreadLoop += _explorer.UpdateExplorer;

        Hierarchy.OnEntitySelected += Inspector.SetSelectedEntity;
    }
}