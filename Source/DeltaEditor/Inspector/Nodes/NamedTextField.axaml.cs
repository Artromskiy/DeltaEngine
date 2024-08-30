using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace DeltaEditor;

public partial class NamedTextField : UserControl
{
    public static readonly StyledProperty<string?> FieldNameProperty =
        AvaloniaProperty.Register<ComponentNodeControl, string?>(nameof(FieldName));

    public static readonly StyledProperty<string?> FieldDataProperty =
        AvaloniaProperty.Register<ComponentNodeControl, string?>(nameof(Data));

    public static readonly StyledProperty<Cursor?> FieldCursorProperty =
        AvaloniaProperty.Register<ComponentNodeControl, Cursor?>(nameof(FieldCursor));

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

    public TextBox FieldData => DataTextBox;

    public NamedTextField() => InitializeComponent();
}