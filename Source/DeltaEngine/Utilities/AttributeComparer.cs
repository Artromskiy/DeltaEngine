using Arch.Core.Utils;
using System;
using System.Collections.Generic;

namespace Delta.Utilities;
public class AttributeComparer<A> : IComparer<ComponentType> where A : Attribute
{
    private static readonly AttributeComparer<A> _defaultComparer = new();
    public static AttributeComparer<A> Default => _defaultComparer;
    public int Compare(ComponentType x, ComponentType y)
    {
        var attr1 = x.Type.GetAttribute<A>();
        var attr2 = y.Type.GetAttribute<A>();
        return Comparer<A>.Default.Compare(attr1, attr2);
    }
}
