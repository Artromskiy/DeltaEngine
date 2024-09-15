using Arch.Core;

namespace Delta.ECS.Components;

/// <summary>
/// Stores information about parent of entity.
/// Can be used for world TRS calculations for rendering
/// or other child/parent dependencies
/// </summary>
public readonly struct ChildOf
{
    /// <summary>
    /// Parent of entity, containing <see cref="ChildOf"/> component
    /// </summary>
    public readonly EntityReference parent;

    public ChildOf(EntityReference parent)
    {
        this.parent = parent;
    }
}