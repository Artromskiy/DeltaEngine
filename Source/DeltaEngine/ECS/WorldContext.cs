using Arch.Core;
using Delta.ECS.Components;
using System.Numerics;

namespace Delta.ECS;

internal readonly struct WorldContext(World world)
{
    [Imp(Inl)]
    public readonly Matrix4x4 GetParentWorldMatrix(Entity entity)
    {
        if (entity.GetParent<Transform>(out var parent))
            return GetWorldRecursive(parent);
        return Matrix4x4.Identity;
    }

    [Imp(Inl)]
    public readonly bool GetParent<T>(Entity entity, out Entity parent)
    {
        parent = entity;
        while (GetParent(ref parent))
            if (world.Has<T>(parent))
                return true;
        return false;
    }


    [Imp(Inl)]
    public readonly Matrix4x4 GetWorldRecursive(Entity entity)
    {
        ref var transform = ref world.Get<Transform>(entity);
        var localMatrix = transform.LocalMatrix;
        if (GetParent<Transform>(entity, out Entity parent))
            return GetWorldRecursive(parent) * localMatrix;
        else
            return localMatrix;
    }

    [Imp(Inl)]
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

    [Imp(Inl)]
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
