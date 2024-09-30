
namespace Delta.ECS.Components;
/// <summary>
/// Stores information about order of entity in hierarchy.
/// Root entity does not have <see cref="ChildOf"/> component,
/// but still contains <see cref="Order"/> component with <see cref="order"/>
/// Each <see cref="order"/> value is unique for each <see cref="ChildOf.parent"/> group
/// </summary>
public struct Order
{
    public int order;

    public Order(int order)
    {
        this.order = order;
    }
}
