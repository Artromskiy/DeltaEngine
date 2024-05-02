using Arch.Core.Utils;
using System;
using System.Collections.Generic;

namespace Delta.Utilities;
public class NullSafeAttributeComparer<A> : IComparer<ComponentType> where A : Attribute
{
    private static readonly NullSafeAttributeComparer<A> _defaultComparer = new();
    public static NullSafeAttributeComparer<A> Default => _defaultComparer;
    public int Compare(ComponentType x, ComponentType y)
    {
        var attr1 = x.Type.GetAttribute<A>();
        var attr2 = y.Type.GetAttribute<A>();
        return NullSafeComparer<A>.Default.Compare(attr2, attr1);
    }
}
