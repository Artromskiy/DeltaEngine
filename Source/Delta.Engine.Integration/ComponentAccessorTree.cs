using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Delta.Engine.Integration;

/// <summary>
/// A safe value-level accessor boundary backed by generated delegates. The
/// backend can later replace expression delegates with source-generated ref
/// accessors without changing the UI-facing contract.
/// </summary>
public sealed class ComponentAccessorTree
{
    private readonly Type _componentType;
    private readonly Dictionary<string, FieldAccessor> _accessors;

    private ComponentAccessorTree(Type componentType, Dictionary<string, FieldAccessor> accessors)
    {
        _componentType = componentType;
        _accessors = accessors;
        Schema = ComponentSchemaBuilder.Create(componentType);
    }

    public ComponentSchema Schema { get; }

    public static ComponentAccessorTree Create<T>() => Create(typeof(T));

    public static ComponentAccessorTree Create(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        var accessors = new Dictionary<string, FieldAccessor>(StringComparer.Ordinal);
        BuildAccessors(componentType, string.Empty, [], accessors);
        return new ComponentAccessorTree(componentType, accessors);
    }

    public bool TryGet(object component, string fieldId, out object? value)
    {
        if (component is null || !_componentType.IsInstanceOfType(component) ||
            !_accessors.TryGetValue(fieldId, out var accessor))
        {
            value = null;
            return false;
        }

        value = accessor.Getter(component);
        return true;
    }

    public bool TrySet(object component, string fieldId, object? value)
    {
        if (component is null || !_componentType.IsInstanceOfType(component) ||
            !_accessors.TryGetValue(fieldId, out var accessor) || accessor.Setter == null ||
            !CanAssign(accessor.FieldType, value))
            return false;

        accessor.Setter(component, value);
        return true;
    }

    private static void BuildAccessors(
        Type type,
        string parentPath,
        HashSet<Type> activeTypes,
        Dictionary<string, FieldAccessor> accessors)
    {
        if (!activeTypes.Add(type))
            return;

        foreach (var field in GetFields(type))
        {
            string fieldId = string.IsNullOrEmpty(parentPath) ? field.Name : $"{parentPath}.{field.Name}";
            var fieldPath = BuildFieldPath(type, field, parentPath, accessors);
            accessors[fieldId] = CreateAccessor(fieldPath, fieldId);
            BuildAccessors(field.FieldType, fieldId, activeTypes, accessors);
        }

        activeTypes.Remove(type);
    }

    private static FieldInfo[] BuildFieldPath(
        Type type,
        FieldInfo field,
        string parentPath,
        Dictionary<string, FieldAccessor> accessors)
    {
        if (string.IsNullOrEmpty(parentPath))
            return [field];

        var parent = accessors[parentPath].Path;
        return [.. parent, field];
    }

    private static FieldAccessor CreateAccessor(FieldInfo[] path, string fieldId)
    {
        var component = Expression.Parameter(typeof(object), "component");
        var value = Expression.Parameter(typeof(object), "value");
        Expression current = Expression.Convert(component, path[0].DeclaringType!);
        foreach (var field in path)
            current = Expression.Field(current, field);

        var getter = Expression.Lambda<Func<object, object?>>(
            Expression.Convert(current, typeof(object)), component).Compile();
        Action<object, object?>? setter = null;
        var fieldInfo = path[^1];
        if (!fieldInfo.IsInitOnly && !fieldInfo.IsLiteral)
        {
            var assignment = Expression.Assign(current, Expression.Convert(value, fieldInfo.FieldType));
            setter = Expression.Lambda<Action<object, object?>>(assignment, component, value).Compile();
        }

        return new FieldAccessor(fieldId, path, fieldInfo.FieldType, getter, setter);
    }

    private static IEnumerable<FieldInfo> GetFields(Type type) => type
        .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .Where(field => !field.IsStatic && (field.IsPublic || IsEditable(field)));

    private static bool IsEditable(FieldInfo field) => field.GetCustomAttributes(inherit: false)
        .Any(attribute => attribute.GetType().Name.Equals("EditableAttribute", StringComparison.Ordinal));

    private static bool CanAssign(Type fieldType, object? value) => value is null
        ? !fieldType.IsValueType || Nullable.GetUnderlyingType(fieldType) != null
        : fieldType.IsInstanceOfType(value);

    private sealed record FieldAccessor(
        string Id,
        FieldInfo[] Path,
        Type FieldType,
        Func<object, object?> Getter,
        Action<object, object?>? Setter);
}
