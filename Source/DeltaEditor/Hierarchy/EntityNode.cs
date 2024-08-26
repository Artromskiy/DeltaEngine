using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;
using DeltaEditor.Inspector.Internal;
using DeltaEditor.Tools;

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
        set => BackgroundColor = value ? NodeConst.SelectedColor : NodeConst.NotSelectedColor;
    }

    private void OnTapped(object? sender, TappedEventArgs eventArgs) => OnClicked?.Invoke(this);

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
