using Arch.Core;
using Arch.Core.Extensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Delta.ECS.Components;
using DeltaEditor;
using System;

namespace DeltaEditor;

public partial class HierarchyNodeControl : UserControl
{
    public static readonly StyledProperty<bool> SelectedProperty =
        AvaloniaProperty.Register<ComponentNodeControl, bool>(nameof(Selected));

    public event Action<HierarchyNodeControl>? OnClicked;
    public EntityReference Entity { get; private set; }

    public bool Selected
    {
        get => GetValue(SelectedProperty);
        set
        {
            SetValue(SelectedProperty, value);
            MainBorder.Background = new SolidColorBrush(value ? Colors.Cyan : Colors.Magenta);
        }
    }

    public HierarchyNodeControl() => InitializeComponent();
    public HierarchyNodeControl(EntityReference entityReference) : this()
    {
        Entity = entityReference;
        NodeName.Content = EntityString(Entity);
        MainBorder.Tapped += OnTapped;
    }
    private void OnTapped(object? sender, TappedEventArgs eventArgs) => OnClicked?.Invoke(this);

    public void UpdateEntity(EntityReference entityReference)
    {
        Entity = entityReference;
        NodeName.Content = EntityString(Entity);
    }

    private static string EntityString(EntityReference entityReference)
    {
        if (!entityReference.IsAlive())
            return string.Empty;
        var entity = entityReference.Entity;
        if (entity.TryGet<EntityName>(out var entityName) && !string.IsNullOrEmpty(entityName.name))
            return entityName.name;
        return entityReference.LookupString();
    }
}