using System;

namespace Delta.Scripting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class ComponentAttribute : Attribute, IComparable<ComponentAttribute>
{
    public readonly bool builtIn;
    public readonly int order;

    public ComponentAttribute(int order = 0) : this(order, false)
    {
        this.order = order;
    }

    internal ComponentAttribute(int order, bool builtIn)
    {
        this.order = order;
        this.builtIn = builtIn;
    }

    public int CompareTo(ComponentAttribute? other)
    {
        Debug.Assert(other != null);

        if (builtIn != other.builtIn)
            return builtIn.CompareTo(other.builtIn);

        return order.CompareTo(other.order);
    }
}