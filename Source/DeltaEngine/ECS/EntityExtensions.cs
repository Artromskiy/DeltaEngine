using Arch.Core;
using Arch.Core.Extensions.Dangerous;
using Arch.Core.Utils;
using Delta.Runtime;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Delta.ECS;

public static class EntityExtensions
{
    public static bool Has<T>(this Entity entity)
    {
        return IRuntimeContext.Current.SceneManager.CurrentScene._world.Has<T>(entity);
    }

    public static void Remove<T>(this Entity entity)
    {
        IRuntimeContext.Current.SceneManager.CurrentScene._world.Remove<T>(entity);
    }

    public static void Remove(this Entity entity, ComponentType cmpType)
    {
        IRuntimeContext.Current.SceneManager.CurrentScene._world.Remove(entity, cmpType);
    }

    public static ReadOnlySpan<ComponentType> GetComponentTypes(this Entity entity)
    {
        return IRuntimeContext.Current.SceneManager.CurrentScene._world.GetComponentTypes(entity);
    }

    public static ref T Get<T>(this Entity entity)
    {
        return ref IRuntimeContext.Current.SceneManager.CurrentScene._world.Get<T>(entity);
    }
    public static void Add<T>(this Entity entity)
    {
        IRuntimeContext.Current.SceneManager.CurrentScene._world.Add<T>(entity);
    }
    public static void Add<T1, T2>(this Entity entity, T1 cmp1, T2 cmp2)
    {
        IRuntimeContext.Current.SceneManager.CurrentScene._world.Add(entity, cmp1, cmp2);
    }
    public static ref T AddOrGet<T>(this Entity entity)
    {
        return ref IRuntimeContext.Current.SceneManager.CurrentScene._world.AddOrGet<T>(entity);
    }
    public static void Add(this Entity entity, in object cmp)
    {
        IRuntimeContext.Current.SceneManager.CurrentScene._world.Add(entity, cmp);
    }

    public static bool Has(this Entity entity, Type componentType)
    {
        return IRuntimeContext.Current.SceneManager.CurrentScene._world.Has(entity, componentType);
    }

    public static ref T TryGetRef<T>(this Entity entity, out bool has)
    {
        return ref IRuntimeContext.Current.SceneManager.CurrentScene._world.TryGetRef<T>(entity, out has);
    }
    public static bool TryGet<T>(this Entity entity, out T? cmp)
    {
        return IRuntimeContext.Current.SceneManager.CurrentScene._world.TryGet(entity, out cmp);
    }
    public static int Version(this Entity entity)
    {
        return IRuntimeContext.Current.SceneManager.CurrentScene._world.Version(entity);
    }

    public static ref byte GetComponentByteRef(this EntityReference entityReference, ComponentType componentType)
    {
        var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
        ref readonly var chunk =ref world.GetChunk(entityReference.Entity);
        (var componentIndex, var _) = world.GetSlot(entityReference.Entity);
        ref byte startRef = ref MemoryMarshal.GetArrayDataReference(chunk.GetArray(componentType));
        return ref Unsafe.AddByteOffset(ref startRef, componentType.ByteSize * componentIndex);
    }
}
