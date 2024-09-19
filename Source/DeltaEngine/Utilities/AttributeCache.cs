using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Delta.Utilities;

public static class AttributeCache
{
    private static readonly ConditionalWeakTable<Type, Dictionary<Type, object?>> _typeToAttributesCache = [];
    private static Dictionary<Type, object?> GetDictionaryOfAttributes(Type type) => _typeToAttributesCache.GetOrCreateValue(type);
    public static A? GetAttribute<A>(this Type type) where A : Attribute
    {
        var attributeType = typeof(A);
        var dictionary = GetDictionaryOfAttributes(type);
        if (!dictionary.TryGetValue(attributeType, out var attribute))
            dictionary[attributeType] = attribute = AttributeGetter<A>(type);
        return attribute as A;
    }
    public static A? GetAttribute<A, T>() where A : Attribute => typeof(T).GetAttribute<A>();
    public static bool HasAttribute<A, T>() where A : Attribute => typeof(T).GetAttribute<A>() != null;
    public static bool HasAttribute<A>(this Type type) where A : Attribute => type.GetAttribute<A>() != null;

    private static A? AttributeGetter<A>(Type objectType) where A : Attribute => objectType.GetCustomAttribute<A>(false);
}