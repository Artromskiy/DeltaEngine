using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;
using Delta.ECS.Components.Hierarchy;
using System;
using System.Numerics;

namespace Delta.ECS;
internal static class ChildOfExtensions
{
    /// <summary>
    /// Use to get World matrix on <see cref="Entity"/>s when existense of <see cref="Transform"/> or <see cref="ChildOf"/> components not known
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>World matrix or <see cref="Matrix4x4.Identity"/> if no <see cref="Transform"/> found</returns>
    [Imp(Inl)]
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
    [Imp(Inl)]
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
    [Imp(Inl)]
    public static Matrix4x4 GetWorldRecursive(this Entity entity)
    {
        ref var transform = ref entity.Get<Transform>();
        var localMatrix = transform.LocalMatrix;
        if (entity.GetParent<Transform>(out Entity parent))
            return parent.GetWorldRecursive() * localMatrix;
        else
            return localMatrix;
    }

    [Imp(Inl)]
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


    [Imp(Inl)]
    public static bool GetParent<T>(this Entity entity, out Entity parent)
    {
        parent = entity;
        while (GetParent(ref parent))
            if (parent.Has<T>())
                return true;
        return false;
    }


    [Imp(Inl)]
    public static bool HasParent<T>(this Entity entity)
    {
        while (GetParent(ref entity))
            if (entity.Has<T>())
                return true;
        return false;
    }

    [Imp(Inl)]
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

    [Imp(Inl)]
    public static bool GetLast<T>(this Entity entity, out Entity last)
    {
        last = Entity.Null;
        while (GetParent(ref entity))
            if (entity.Has<T>())
                last = entity;
        return last != Entity.Null;
    }


    [Imp(Inl)]
    public static bool Has(this World world, in QueryDescription queryDescription)
    {
        var query = world.Query(in queryDescription);
        foreach (var archetype in query.GetArchetypeIterator())
            if (archetype.EntityCount > 0)
                return true;
        return false;
    }
}