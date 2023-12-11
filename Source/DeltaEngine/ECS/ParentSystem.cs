using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using System;
using System.Collections.Generic;

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
        _world.SubscribeComponentAdded<ChildOf>(OnBecomeChild);
        _world.SubscribeComponentRemoved<ChildOf>(OnStopChild);
        _world.SubscribeComponentSet<ChildOf>(OnComponentChanged);
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

    private void InvokeSubscribers(in Entity entity, ref ChildOf childOf)
    {
        for (int i = 0; i < _subscriberTypes.Count; i++)
        {
            var types = _subscriberTypes[i];
            var parentType = types.parent;
            var childType = types.child;
            bool selfSearch = childType.Equals(parentType); // TODO think about it, current setup will now work for Transform to Transform
            if (_world.Has(entity, parentType)) // Skip, as entity self is type of parent, so dependency from child to parent not changed in down branches of tree
                continue;
            if (!GetParent(entity, parentType, out var parent)) // Skip, as entity of parent type not found upper, so dependency from child to parent not changed
                continue;
            if (!_parents.TryGetValue(entity, out var childs)) // Skip, sa luckily entity hasn't got any childs
                continue;

            // THE HELL BEGINS

            var subscriber = _subscribers[i];
            Stack<Entity> _toDig = [];
            foreach (var child in childs)
                _toDig.Push(child);
            while (_toDig.Count > 0)
            {
                var child = _toDig.Pop();
                if (_world.Has(child, parentType)) // node is type of parent, dependency from other childs reference to it and not changed
                    continue;
                if (!_world.Has(child, childType)) // node is not type of child, keep searching other child nodes
                {
                    if (_parents.TryGetValue(child, out childs))
                        foreach (var newChild in childs)
                            _toDig.Push(newChild);
                }
                else
                    subscriber.OnChangedParent(parent, child); // node is required type, invoke subscriber event
            }
        }
    }

    private void OnStopChild(in Entity entity, ref ChildOf component)
    {
        Debug.Assert(_parents.ContainsKey(component.parent.Entity));
        _parents[component.parent.Entity].Remove(entity);
    }

    private void OnComponentChanged(in Entity entity, ref ChildOf component)
    {

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
