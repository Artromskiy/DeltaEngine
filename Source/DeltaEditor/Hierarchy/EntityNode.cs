﻿using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;

namespace DeltaEditor.Hierarchy;

internal class EntityNode : ContentView
{
    private readonly Label Name = new();
    public EntityReference Entity { get; private set; }

    public event Action<EntityNode>? OnClicked;

    public EntityNode(EntityReference entity)
    {
        Entity = entity;
        Name.Text = EntityString(Entity);
        var gesture = new TapGestureRecognizer();
        gesture.Tapped += OnTapped;
        GestureRecognizers.Add(gesture);
        Content = Name;
    }

    public void UpdateEntity(EntityReference entity)
    {
        Entity = entity;
        Name.Text = EntityString(Entity);
    }

    public bool Selected
    {
        set => BackgroundColor = value ? Color.FromRgb(20, 5, 30) : Color.FromRgba(0, 0, 0, 0);
    }

    private void OnTapped(object? sender, TappedEventArgs eventArgs) => OnClicked?.Invoke(this);

    private static string EntityString(EntityReference entityReference)
    {
        if (!entityReference.IsAlive())
            return string.Empty;
        var entity = entityReference.Entity;
        if (entity.TryGet<EntityName>(out var entityName) && !string.IsNullOrEmpty(entityName.name))
            return entityName.name;
        return $"id: {entity.Id}, ver: {entityReference.Version}";
    }
}
