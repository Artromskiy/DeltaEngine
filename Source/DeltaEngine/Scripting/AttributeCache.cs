using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Delta.Scripting;

public static class AttributeCache
{
    private static readonly ConditionalWeakTable<Type, ConcurrentDictionary<Type, object?>> _typeToAttributesCache = [];

    private static ConcurrentDictionary<Type, object?> GetDictionaryOfAttributes(Type type)=> _typeToAttributesCache.GetOrCreateValue(type);
    public static A? GetAttribute<A>(this Type type) where A : Attribute => GetDictionaryOfAttributes(type).GetOrAdd(typeof(A), AttributeGetter<A>, type) as A;
    public static A? GetAttribute<A, T>() where A : Attribute => GetAttribute<A>(typeof(T));
    public static bool HasAttribute<A, T>() where A : Attribute => GetAttribute<A>(typeof(T)) != null;
    public static bool HasAttribute<A>(this Type type) where A : Attribute => GetAttribute<A>(type) != null;

    private static A? AttributeGetter<A>(Type attributeType, Type objectType) where A : Attribute => objectType.GetCustomAttribute<A>(false);
}