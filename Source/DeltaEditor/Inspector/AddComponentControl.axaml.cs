using Avalonia.Controls;
using System;
using System.Collections.Generic;

namespace DeltaEditor;

public partial class AddComponentControl : UserControl
{
    public event Action<Type> OnComponentAddRequested;

    private readonly Dictionary<Type, AddComponentItemControl> _createdItems = [];

    public AddComponentControl() => InitializeComponent();

    public void UpdateComponents(HashSet<Type> types)
    {
        ComponentsStackPanel.Children.Clear();
        foreach (var type in types)
            ComponentsStackPanel.Children.Add(GetOrCreateItem(type));
    }

    private AddComponentItemControl GetOrCreateItem(Type type)
    {
        if (!_createdItems.TryGetValue(type, out var item))
        {
            _createdItems[type] = item = new AddComponentItemControl(type);
            item.OnClick += OnComponentAddRequested;
        }
        return item;
    }
}