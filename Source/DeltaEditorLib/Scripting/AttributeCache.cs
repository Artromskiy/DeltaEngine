using System.Reflection;
using System.Runtime.CompilerServices;

namespace DeltaEditorLib.Scripting;

public static class AttributeCache
{
    private static readonly ConditionalWeakTable<Type, Dictionary<Type, object>> _typeToAttributesCache = [];

    private static Dictionary<Type, object> GetDictionaryOfAttributes(Type type)
    {
        if (!_typeToAttributesCache.TryGetValue(type, out var dictionaryOfAttributes))
            _typeToAttributesCache.Add(type, dictionaryOfAttributes = []);
        return dictionaryOfAttributes;
    }
    public static A? GetAttribute<A, T>() where A : Attribute => GetAttribute<A>(typeof(T));
    public static A? GetAttribute<A>(this Type type) where A : Attribute
    {
        var dictionaryOfAttributes = GetDictionaryOfAttributes(type);
        var attributeType = typeof(A);
        if (!dictionaryOfAttributes.TryGetValue(attributeType, out var attribute))
        {
            attribute = type.GetCustomAttribute<A>(false);
            if (attribute != null)
                dictionaryOfAttributes.Add(attributeType, attribute);
        }
        return attribute as A;
    }
}