using Arch.Core;

namespace Delta.ECS.Components;

public struct ChildOf
{
    public EntityReference parent;

    public ChildOf(EntityReference parent)
    {
        this.parent = parent;
    }
}