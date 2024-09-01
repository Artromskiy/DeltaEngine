using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DeltaEditor;

public partial class EditorBar : UserControl
{
    public EditorBar()
    {
        InitializeComponent();
        PlayButton.IsCheckedChanged += PlayChanged;
        PauseButton.IsCheckedChanged += PauseChanged;
        NextButton.Click += NextClicked;
    }

    private void PlayChanged(object? sender, RoutedEventArgs e)
    {
        var value = PlayButton.IsChecked ?? default;
        if (!value)
            PauseButton.IsChecked = false;

        UpdateRuntimeState();
    }
    private void PauseChanged(object? sender, RoutedEventArgs e)
    {
        UpdateRuntimeState();
    }

    private void NextClicked(object? sender, RoutedEventArgs e)
    {
        PauseButton.IsChecked = true;
        PlayButton.IsChecked = true;
    }

    private void UpdateRuntimeState()
    {
        if (Design.IsDesignMode)
            return;

        var running = PlayButton.IsChecked ?? default;
        var pausing = PauseButton.IsChecked ?? default;
        Program.RuntimeLoader.OnRuntimeThread += r => r.SceneManager.Running = running && !pausing;
    }
}