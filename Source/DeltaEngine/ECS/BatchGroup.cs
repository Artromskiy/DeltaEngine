using DeltaEngine.Rendering;

namespace DeltaEngine.ECS;
public struct BatchGroup
{
    internal int id;
    internal StackList<Transform> group;
}
