using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace DeltaEditor;

public partial class AddComponentItemControl : UserControl
{
    public event Action<Type>? OnClick;

    public AddComponentItemControl()
    {
        InitializeComponent();
        Tapped += OnTap;
        PointerEntered += OnHighlight;
        PointerExited += OnDehighlight;
    }

    private readonly Type _componentType;
    public AddComponentItemControl(Type type) : this()
    {
        _componentType = type;
        ComponentNameLabel.Content = _componentType.ToString();
    }


    private void OnTap(object? sender, TappedEventArgs e)
    {
        OnClick?.Invoke(_componentType);
    }

    private void OnHighlight(object? sender, PointerEventArgs e)
    {

    }

    private void OnDehighlight(object? sender, PointerEventArgs e)
    {

    }
}