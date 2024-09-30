using Arch.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;
using System;

namespace DeltaEditor;

internal partial class NamedTextField : InspectorNode
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

    public event Action<float>? OnDrag;

    public TextBox FieldData => DataTextBox;

    public NamedTextField()=> InitializeComponent();

    private void DataTextBox_GotFocus(object? sender, GotFocusEventArgs e)
    {
        IColorMarkable.MarkedNode = this;
    }

    private void DataTextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (IColorMarkable.MarkedNode == this)
            IColorMarkable.MarkedNode = null;
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

        OnDrag?.Invoke((float)deltaPos.X);
    }

    public override bool UpdateData(ref EntityReference entity) => false;
    public override void SetLabelColor(IBrush brush) => NameLabel.Foreground = brush;
}