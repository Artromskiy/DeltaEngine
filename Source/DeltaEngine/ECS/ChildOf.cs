using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Delta.ECS;

internal struct ChildOf(EntityReference parent)
{
    public EntityReference parent = parent;
}

internal readonly struct WorldContext(World world)
{

    [MethodImpl(Inl)]
    public readonly Matrix4x4 GetParentWorldMatrix(Entity entity)
    {
        if (entity.GetParent<Transform>(out var parent))
            return GetWorldRecursive(parent);
        return Matrix4x4.Identity;
    }

    [MethodImpl(Inl)]
    public readonly bool GetParent<T>(Entity entity, out Entity parent)
    {
        parent = entity;
        while (GetParent(ref parent))
            if (world.Has<T>(parent))
                return true;
        return false;
    }


    [MethodImpl(Inl)]
    public readonly Matrix4x4 GetWorldRecursive(Entity entity)
    {
        ref var transform = ref world.Get<Transform>(entity);
        var localMatrix = transform.LocalMatrix;
        if (GetParent<Transform>(entity, out Entity parent))
            return GetWorldRecursive(parent) * localMatrix;
        else
            return localMatrix;
    }

    [MethodImpl(Inl)]
    public readonly bool GetParent(ref Entity entity)
    {
        ref var childOf = ref world.TryGetRef<ChildOf>(entity, out bool has);
        if (has)
        {
            entity = childOf.parent;
            return world.Version(entity) == childOf.parent.Version;
        }
        return false;
    }

    [MethodImpl(Inl)]
    public readonly bool HasParent<T>(Entity entity)
    {
        do
        {
            if (world.Has<T>(entity))
                return true;
        } while (GetParent(ref entity));

        return false;
    }

    public readonly bool Has<T>(Entity entity) => world.Has<T>(entity);
}

internal static class ChildOfExtensions
{
    /// <summary>
    /// Use to get World matrix on <see cref="Entity"/>s when existense of <see cref="Transform"/> or <see cref="ChildOf"/> components not known
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>World matrix or <see cref="Matrix4x4.Identity"/> if no <see cref="Transform"/> found</returns>
    [MethodImpl(Inl)]
    public static Matrix4x4 GetWorldMatrix(this in Entity entity)
    {
        if (entity.Has<Transform>())
            return GetWorldRecursive(entity);
        else
            return GetParentWorldMatrix(entity);
    }

    /// <summary>
    /// Use for <see cref="Entity"/> without <see cref="Transform"/> component but with <see cref="ChildOf"/>
    /// component containing reference to parent
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>World matrix or <see cref="Matrix4x4.Identity"/> if no parent with <see cref="Transform"/> found</returns>
    [MethodImpl(Inl)]
    public static Matrix4x4 GetParentWorldMatrix(this in Entity entity)
    {
        if (entity.GetParent<Transform>(out var parent))
            return parent.GetWorldRecursive();
        return Matrix4x4.Identity;
    }

    /// <summary>
    /// Use for <see cref="Entity"/>> with transform
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>World matrix</returns>
    [MethodImpl(Inl)]
    public static Matrix4x4 GetWorldRecursive(this Entity entity)
    {
        ref var transform = ref entity.Get<Transform>();
        var localMatrix = transform.LocalMatrix;
        if (entity.GetParent<Transform>(out Entity parent))
            return parent.GetWorldRecursive() * localMatrix;
        else
            return localMatrix;
    }

    [MethodImpl(Inl)]
    public static bool GetParent(this ref Entity entity)
    {
        ref var childOf = ref entity.TryGetRef<ChildOf>(out bool has);
        if (has)
        {
            entity = childOf.parent;
            return entity.Version() == childOf.parent.Version;
        }
        return false;
    }


    [MethodImpl(Inl)]
    public static bool GetParent<T>(this Entity entity, out Entity parent)
    {
        parent = entity;
        while (GetParent(ref parent))
            if (parent.Has<T>())
                return true;
        return false;
    }


    [MethodImpl(Inl)]
    public static bool HasParent<T>(this Entity entity)
    {
        while (GetParent(ref entity))
            if (entity.Has<T>())
                return true;
        return false;
    }

    [MethodImpl(Inl)]
    public static uint GetDepth<T>(this Entity entity)
    {
        uint depth = 0;
        while (GetParent(ref entity))
            if (entity.Has<T>())
                depth++;
        return depth;
    }

    public static void WriteDepth<T>(this Entity entity, Span<T> depthSpan)
    {
        int depth = 0;
        while (GetParent(ref entity))
            if (entity.Has<T>())
                depthSpan[depth++] = entity.Get<T>();
    }

    [MethodImpl(Inl)]
    public static bool GetLast<T>(this Entity entity, out Entity last)
    {
        last = Entity.Null;
        while (GetParent(ref entity))
            if (entity.Has<T>())
                last = entity;
        return last != Entity.Null;
    }


    public static bool Has(this World world, in QueryDescription queryDescription)
    {
        var query = world.Query(in queryDescription);
        foreach (var archetype in query.GetArchetypeIterator())
            if (archetype.Entities > 0)
                return true;
        return false;
    }
}