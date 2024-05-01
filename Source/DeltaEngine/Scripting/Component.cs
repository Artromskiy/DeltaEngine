using System;

namespace Delta.Scripting;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ComponentAttribute : Attribute
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

    public static int Compare(ComponentAttribute? attr1, ComponentAttribute? attr2)
    {
        if (attr1 == null || attr2 == null)
            return NullCompare(attr1, attr2);

        if (attr1.builtIn != attr2.builtIn)
            return attr1.builtIn.CompareTo(attr2.builtIn);

        return attr1.order.CompareTo(attr2.order);
    }

    private static int NullCompare(object? obj1, object? obj2)
    {
        return (obj1, obj2) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            _ => 0
        };
    }
}