using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeltaEngine.ECS;
internal class ParentSystem
{
    private readonly World _world;
    private readonly Dictionary<Entity, HashSet<Entity>> _parents = [];
    private readonly List<(Type parent, Type child)> _subscriberTypes = [];
    private readonly List<IParentSubscriber> _subscribers = [];

    public ParentSystem(World world)
    {
        _world = world;
    }

    public void AddSubscriber<P, K>(IParentSubscriber subscriber)
    {
        _subscriberTypes.Add((typeof(P), typeof(K)));
        _subscribers.Add(subscriber);
    }

    private void OnBecomeChild(in Entity entity, ref ChildOf component)
    {
        if (!_parents.TryGetValue(component.parent.Entity, out var childs))
            childs = [];
        childs.Add(entity);
    }

    private void OnStopChild(in Entity entity, ref ChildOf component)
    {
        Debug.Assert(_parents.ContainsKey(component.parent.Entity));
        _parents[component.parent.Entity].Remove(entity);
    }


    private bool GetParent(in Entity entity, out Entity parent)
    {
        ref var childOf = ref _world.TryGetRef<ChildOf>(entity, out bool hasParent);
        if (!hasParent || !childOf.parent.IsAlive())
        {
            parent = default;
            return false;
        }
        parent = childOf.parent.Entity;
        return true;
    }

    private bool GetParent(in Entity entity, ComponentType type, out Entity parent)
    {
        while (GetParent(in entity, out parent))
            if (_world.Has(parent, type))
                return true;
        return false;
    }
}

public interface IParentSubscriber
{
    void OnStartChildOf(Entity parent);
    void OnStopChildOf(Entity parent);
    void OnChangedParent(Entity parent, Entity child);
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

    public static bool GetParent<T>(this ChildOf child, out T component)
    {
        component = default!;
        ref ChildOf childOf = ref Unsafe.NullRef<ChildOf>();
        Entity parent = child.parent;
        bool hasParent = parent.IsAlive();
        while (hasParent)
        {
            if (parent.TryGet(out component))
                return true;
            childOf = parent.TryGetRef<ChildOf>(out hasParent);
            parent = hasParent ? childOf.parent : parent;
            hasParent = hasParent && parent.IsAlive();
        }
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