using Delta.Scripting;

namespace Delta.ECS.Components;

[Component]
public struct EntityName
{
    public string name;
    public EntityName(string name) => this.name = name;
}