using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Delta.ECS.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class ComponentAttribute : Attribute, IComparable<ComponentAttribute>
{
    public readonly bool builtIn;
    public readonly int order;

    public readonly int sourceLineNumber;
    public readonly string sourceFilePath;

    internal ComponentAttribute(int order, bool builtIn, [CallerLineNumber] int sourceLineNumber = 0, [CallerFilePath] string sourceFilePath = "")
    {
        this.order = order;
        this.builtIn = builtIn;
        this.sourceLineNumber = sourceLineNumber;
        this.sourceFilePath = sourceFilePath;
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