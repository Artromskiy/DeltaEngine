using Delta.Scripting;

namespace Delta.ECS.Components;

[Component]
public struct EntityName
{
    public string text;
    public EntityName(string name) => text = name;
}