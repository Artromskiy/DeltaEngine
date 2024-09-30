using Arch.Core;
using Arch.Core.Extensions.Dangerous;
using Arch.Core.Utils;
using Delta.ECS;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DeltaEditorLib.Scripting;

public static class AccessorContainerExtensions
{
    public static Type GetFieldType(this IAccessorsContainer container, Type type, ReadOnlySpan<string> path)
    {
        foreach (var fieldName in path)
            type = container.AllAccessors[type].GetFieldType(fieldName);
        return type;
    }

    public static unsafe K GetComponentFieldValue<K>(this IAccessorsContainer container, EntityReference entityReference, Type componentType, ReadOnlySpan<string> path)
    {
        ref byte cmpRef = ref entityReference.GetComponentByteRef(componentType);
        return container.GetFieldValue<K>(componentType, new(Unsafe.AsPointer(ref cmpRef)), path);
    }

    public static unsafe void SetComponentFieldValue<K>(this IAccessorsContainer container, EntityReference entityReference, Type componentType, ReadOnlySpan<string> path, K value)
    {
        ref byte cmpRef = ref entityReference.GetComponentByteRef(componentType);
        container.SetFieldValue(componentType, new(Unsafe.AsPointer(ref cmpRef)), path, value);
    }

    public static unsafe K GetFieldValue<K>(this IAccessorsContainer container, Type type, nint ptr, ReadOnlySpan<string> path)
    {
        foreach (var fieldName in path)
        {
            var accessor = container.AllAccessors[type];
            ptr = accessor.GetFieldPtr(ptr, fieldName);
            type = accessor.GetFieldType(fieldName);
        }

        if (typeof(K) == type)
            return Unsafe.AsRef<K>(ptr.ToPointer());
        throw new InvalidOperationException();
    }

    public static unsafe void SetFieldValue<K>(this IAccessorsContainer container, Type type, nint ptr, ReadOnlySpan<string> path, K value)
    {
        foreach (var fieldName in path)
        {
            var accessor = container.AllAccessors[type];
            ptr = accessor.GetFieldPtr(ptr, fieldName);
            type = accessor.GetFieldType(fieldName);
        }

        if (typeof(K) == type)
            Unsafe.AsRef<K>(ptr.ToPointer()) = value;
    }
}