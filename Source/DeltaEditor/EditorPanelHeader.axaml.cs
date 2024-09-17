using Avalonia;
using Avalonia.Controls;
using System;

namespace DeltaEditor;

public partial class EditorPanelHeader : UserControl
{
    public static readonly StyledProperty<string?> PanelNameProperty =
        AvaloniaProperty.Register<ComponentNodeControl, string?>(nameof(PanelName));

    public static readonly StyledProperty<string?> PanelIconProperty =
        AvaloniaProperty.Register<ComponentNodeControl, string?>(nameof(PanelName));

    public static readonly StyledProperty<bool> DebugEnabledProperty =
        AvaloniaProperty.Register<ComponentNodeControl, bool>(nameof(DebugEnabled), true);

    public static readonly StyledProperty<bool> CloseEnabledProperty =
        AvaloniaProperty.Register<ComponentNodeControl, bool>(nameof(CloseEnabled), false);

    private void CloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => OnCloseClick?.Invoke();
    public event Action OnCloseClick;

    public string? PanelName
    {
        get => GetValue(PanelNameProperty);
        set
        {
            SetValue(PanelNameProperty, value);
            PanelNameLabel.Content = value;
        }
    }
    public string? PanelIcon
    {
        get => GetValue(PanelIconProperty);
        set
        {
            SetValue(PanelIconProperty, value);
            PanelIconSvg.Path = value;

        }
    }
    public bool DebugEnabled
    {
        get => GetValue(DebugEnabledProperty);
        set
        {
            SetValue(DebugEnabledProperty, value);
            DebugTimer.IsVisible = value;
        }
    }
    public bool CloseEnabled
    {
        get => GetValue(CloseEnabledProperty);
        set
        {
            SetValue(CloseEnabledProperty, value);
            CloseButton.IsVisible = value;
        }
    }

    public void StartDebug()
    {
        if (DebugEnabled)
            DebugTimer.StartDebug();
    }

    public void StopDebug()
    {
        if (DebugEnabled)
            DebugTimer.StopDebug();
    }

    public EditorPanelHeader() => InitializeComponent();

}