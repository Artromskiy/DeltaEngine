using System;

namespace Delta.ECS.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class ComponentAttribute : Attribute, IComparable<ComponentAttribute>
{
    public readonly bool builtIn;
    public readonly int order;
    internal ComponentAttribute(int order, bool builtIn)
    {
        this.order = order;
        this.builtIn = builtIn;
    }

    public ComponentAttribute(int order = 0) : this(order, false) { }

    public int CompareTo(ComponentAttribute? other)
    {
        if (other == null)
            return -1;

        var builtInCompare = builtIn.CompareTo(other.builtIn);
        var orderCompare = order.CompareTo(other.order);

        return builtInCompare != 0 ? builtInCompare : orderCompare;
    }
}