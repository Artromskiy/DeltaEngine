using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;

namespace DeltaEditor;

public class HierarchyEntity : ContentView
{
    private readonly string Name;
    private readonly EntityReference Entity;

    public HierarchyEntity(EntityReference entity)
    {
        Entity = entity;
        Name = EntityString(Entity);
    }

    private static string EntityString(EntityReference entityReference)
    {
        if (!entityReference.IsAlive())
            return string.Empty;
        var entity = entityReference.Entity;
        if (entity.TryGet<EntityName>(out var entityName))
            return entityName.name;
        return $"id: {entity.Id}, ver: {entityReference.Version}";
    }
}
