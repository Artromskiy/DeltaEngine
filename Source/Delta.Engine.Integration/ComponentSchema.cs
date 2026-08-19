using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Delta.Engine.Integration;

[Flags]
public enum ComponentFieldAccess : byte
{
    None = 0,
    Read = 1,
    Write = 2,
}

public sealed record ComponentFieldSchema(
    string Id,
    string Name,
    Type FieldType,
    ComponentFieldAccess Access,
    IReadOnlyList<string> Attributes,
    IReadOnlyList<ComponentFieldSchema> Children);

public sealed record ComponentSchema(
    Type ComponentType,
    IReadOnlyList<ComponentFieldSchema> Fields);

public static class ComponentSchemaBuilder
{
    public static ComponentSchema Create<T>() => Create(typeof(T));

    public static ComponentSchema Create(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        return new ComponentSchema(componentType, BuildFields(componentType, string.Empty, []));
    }

    private static IReadOnlyList<ComponentFieldSchema> BuildFields(
        Type type,
        string parentPath,
        HashSet<Type> activeTypes)
    {
        if (!activeTypes.Add(type))
            return [];

        var fields = type
            .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(field => !field.IsStatic && (field.IsPublic || IsEditable(field)))
            .Select(field =>
            {
                string id = string.IsNullOrEmpty(parentPath) ? field.Name : $"{parentPath}.{field.Name}";
                var access = ComponentFieldAccess.Read;
                if (!field.IsInitOnly && !field.IsLiteral)
                    access |= ComponentFieldAccess.Write;

                var attributes = field.GetCustomAttributes(inherit: false)
                    .Select(attribute => attribute.GetType().FullName ?? attribute.GetType().Name)
                    .ToArray();
                var children = BuildFields(field.FieldType, id, activeTypes);
                return new ComponentFieldSchema(id, field.Name, field.FieldType, access, attributes, children);
            })
            .ToArray();

        activeTypes.Remove(type);
        return fields;
    }

    private static bool IsEditable(FieldInfo field) => field.GetCustomAttributes(inherit: false)
        .Any(attribute => attribute.GetType().Name.Equals("EditableAttribute", StringComparison.Ordinal));
}
