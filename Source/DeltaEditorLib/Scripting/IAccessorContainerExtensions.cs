using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Extensions.Dangerous;
using Arch.Core.Utils;
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

    public static unsafe K GetComponentFieldValue<K>(this IAccessorsContainer container, EntityReference entityReference, ComponentType componentType, ReadOnlySpan<string> path)
    {
        ref readonly var chunk = ref entityReference.Entity.GetChunk();
        (var componentIndex, var _) = World.Worlds[entityReference.Entity.WorldId].GetSlot(entityReference.Entity);
        ref byte startRef = ref MemoryMarshal.GetArrayDataReference(chunk.GetArray(componentType));
        ref byte cmpRef = ref Unsafe.AddByteOffset(ref startRef, componentType.ByteSize * componentIndex);
        return container.GetFieldValue<K>(componentType.Type, new(Unsafe.AsPointer(ref cmpRef)), path);
    }

    public static unsafe void SetComponentFieldValue<K>(this IAccessorsContainer container, EntityReference entityReference, ComponentType componentType, ReadOnlySpan<string> path, K value)
    {
        ref readonly var chunk = ref entityReference.Entity.GetChunk();
        (var componentIndex, var _) = World.Worlds[entityReference.Entity.WorldId].GetSlot(entityReference.Entity);
        ref byte startRef = ref MemoryMarshal.GetArrayDataReference(chunk.GetArray(componentType));
        ref byte cmpRef = ref Unsafe.AddByteOffset(ref startRef, componentType.ByteSize * componentIndex);
        container.SetFieldValue(componentType.Type, new(Unsafe.AsPointer(ref cmpRef)), path, value);
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