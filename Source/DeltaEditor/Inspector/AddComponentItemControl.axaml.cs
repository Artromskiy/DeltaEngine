using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace DeltaEditor;

public partial class AddComponentItemControl : UserControl
{
    private bool _selected;
    public event Action<Type>? OnClick;
    public readonly Type ComponentType;

    public AddComponentItemControl()
    {
        InitializeComponent();
        Tapped += OnTap;
    }

    public AddComponentItemControl(Type type, Action<Type> onClick) : this()
    {
        ComponentType = type;
        ComponentNameLabel.Content = ComponentType.ToString();
        OnClick += onClick;
    }

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            BorderBrush = value ? Tools.Colors.FocusedBrush : Tools.Colors.UnfocusedBorderBrush;
        }
    }

    private void OnTap(object? sender, TappedEventArgs e)
    {
        OnClick?.Invoke(ComponentType);
    }

    private void UserControl_PointerEntered(object? sender, PointerEventArgs e)
    {
        if(!_selected)
            BorderBrush = Tools.Colors.FocusedBrush;
    }

    private void UserControl_PointerExited(object? sender, PointerEventArgs e)
    {
        if(!_selected)
            BorderBrush = Tools.Colors.UnfocusedBorderBrush;
    }
}