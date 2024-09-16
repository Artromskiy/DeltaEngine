using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

public partial class NamedTextField : UserControl
{
    public static readonly StyledProperty<string?> FieldNameProperty =
        AvaloniaProperty.Register<ComponentNodeControl, string?>(nameof(FieldName));

    public static readonly StyledProperty<string?> FieldDataProperty =
        AvaloniaProperty.Register<ComponentNodeControl, string?>(nameof(Data));

    public static readonly StyledProperty<Cursor?> FieldCursorProperty =
        AvaloniaProperty.Register<ComponentNodeControl, Cursor?>(nameof(FieldCursor));

    public static readonly StyledProperty<HorizontalAlignment> FieldNameAlignmentProperty =
        AvaloniaProperty.Register<ComponentNodeControl, HorizontalAlignment>(nameof(FieldCursor));

    public Cursor? FieldCursor
    {
        get => GetValue(FieldCursorProperty);
        set
        {
            SetValue(FieldCursorProperty, value);
            NameLabel.Cursor = value;
        }
    }

    public string? FieldName
    {
        get => GetValue(FieldNameProperty);
        set
        {
            SetValue(FieldNameProperty, value);
            NameLabel.Content = value;
            ContainerGrid.ColumnDefinitions = string.IsNullOrEmpty(value) ?
                [new ColumnDefinition(0, GridUnitType.Pixel), new ColumnDefinition(1, GridUnitType.Star)] :
                [new ColumnDefinition(1, GridUnitType.Star), new ColumnDefinition(3, GridUnitType.Star)];
        }
    }

    public string? Data
    {
        get => GetValue(FieldDataProperty);
        set
        {
            SetValue(FieldDataProperty, value);
            DataTextBox.Text = value;
        }
    }

    public HorizontalAlignment FieldNameAlignment
    {
        get => GetValue(FieldNameAlignmentProperty);
        set
        {
            SetValue(FieldNameAlignmentProperty, value);
            NameLabel.HorizontalAlignment = value;
        }
    }

    public void SetFieldColor(IBrush brush) => NameLabel.Foreground = brush;

    public TextBox FieldData => DataTextBox;

    public NamedTextField()
    {
        InitializeComponent();
    }

    private void DataTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        foreach (var item in this.GetVisualAncestors())
            if (item is InspectorNode node)
                node.SetLabelColor(Tools.Colors.FocusedLabelBrush);
    }

    private void DataTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        foreach (var item in this.GetVisualAncestors())
            if (item is InspectorNode node)
                node.SetLabelColor(Tools.Colors.UnfocusedLabelBrush);
    }
}