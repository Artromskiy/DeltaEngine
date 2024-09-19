using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using DeltaEditor.Inspector.Internal;
using System;

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
        AvaloniaProperty.Register<ComponentNodeControl, HorizontalAlignment>(nameof(FieldNameAlignment));

    private bool _dragging = false;
    private Point _prevPosition;

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
                [new ColumnDefinition(1, GridUnitType.Star), new ColumnDefinition(4, GridUnitType.Star)];
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

    private void SetFieldColor(IBrush brush) => NameLabel.Foreground = brush;
    public event Action<float>? OnDrag;

    public TextBox FieldData => DataTextBox;

    public NamedTextField()
    {
        InitializeComponent();
    }

    private void DataTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        var brush = Tools.Colors.FocusedBrush;
        SetFieldColor(brush);
        foreach (var item in this.GetVisualAncestors())
            if (item is InspectorNode node)
                node.SetLabelColor(brush);
    }

    private void DataTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var brush = Tools.Colors.UnfocusedLabelBrush;
        SetFieldColor(brush);
        foreach (var item in this.GetVisualAncestors())
            if (item is InspectorNode node)
                node.SetLabelColor(brush);
    }

    private void BeginDrag(object? sender, PointerPressedEventArgs e)
    {
        _dragging = true;
        _prevPosition = e.GetPosition(this);
        OnDrag?.Invoke(0);
    }

    private void EndDrag(object? sender, PointerReleasedEventArgs e) => _dragging = false;

    private void Drag(object? sender, PointerEventArgs e)
    {
        if (!_dragging)
            return;

        var pos = e.GetPosition(this);
        var deltaPos = pos - _prevPosition;

        _prevPosition = pos;

        OnDrag?.Invoke((float)(deltaPos.X));
    }
}