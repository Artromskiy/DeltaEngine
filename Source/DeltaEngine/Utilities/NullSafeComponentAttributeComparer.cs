using Arch.Core.Utils;
using System;
using System.Collections.Generic;

namespace Delta.Utilities;
public class NullSafeComponentAttributeComparer<A> : IComparer<ComponentType> where A : Attribute
{
    private static readonly NullSafeComponentAttributeComparer<A> _defaultComparer = new();
    public static NullSafeComponentAttributeComparer<A> Default => _defaultComparer;
    public int Compare(ComponentType x, ComponentType y)
    {
        var attr1 = x.Type.GetAttribute<A>();
        var attr2 = y.Type.GetAttribute<A>();
        return NullSafeComparer<A>.Default.Compare(attr2, attr1);
    }
}