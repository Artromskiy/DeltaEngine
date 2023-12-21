using Arch.Core;
using Arch.Core.Extensions;
using System.Runtime.CompilerServices;

namespace Delta.ECS;

internal struct ChildOf
{
    public EntityReference parent;
}

internal static class ChildOfExtensions
{
    public static bool GetParent(this in Entity entity, out Entity parent)
    {
        ref var childOf = ref entity.TryGetRef<ChildOf>(out bool hasParent);
        if (!hasParent || !childOf.parent.IsAlive())
        {
            parent = default;
            return false;
        }
        parent = childOf.parent.Entity;
        return true;
    }

    public static bool GetParent<T>(this ref ChildOf child, out T component)
    {
        ref ChildOf childOf = ref Unsafe.NullRef<ChildOf>();
        Entity parent = child.parent;
        bool hasParent = parent.IsAlive();
        while (hasParent)
        {
            if (parent.TryGet(out component!))
                return true;
            childOf = parent.TryGetRef<ChildOf>(out hasParent);
            parent = hasParent ? childOf.parent : parent;
            hasParent = hasParent && parent.IsAlive();
        }
        component = default!;
        return false;
    }

    public static bool GetParent<T>(this in Entity entity, out Entity parent)
    {
        while (GetParent(in entity, out parent))
        {
            if (parent.Has<T>())
                return true;
        }
        return false;
    }
}