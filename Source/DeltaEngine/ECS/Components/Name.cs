using Delta.ECS.Attributes;

namespace Delta.ECS.Components;

[Component, Dirty]
public struct EntityName
{
    public string name;
    public EntityName(string name) => this.name = name;
    public EntityName() : this(string.Empty) { }
}