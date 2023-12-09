using Arch.Core;
using DeltaEngine.Collections;
using DeltaEngine.Rendering;
using System;
using System.Collections.Generic;

namespace DeltaEngine.ECS;
internal class GpuMappedParenting<P, C> : StorageDynamicArray<uint> where P : unmanaged
{
    private readonly World _world;
    Dictionary<Entity, HashSet<ChildOf>> _parents = [];

    public GpuMappedParenting(World world, RenderBase renderData) : base(renderData, 1)
    {
        _world = world;

        _world.SubscribeComponentAdded<VersId<P>>(OnParentAdded);
        _world.SubscribeComponentAdded<ChildOf>(OnBecomeChild);
        _world.SubscribeComponentRemoved<ChildOf>(OnStopChild);
        _world.SubscribeComponentSet<ChildOf>(OnComponentChanged);
    }


    private void OnParentAdded(in Entity entity, ref VersId<P> parent)
    {

    }

    private void OnBecomeChild(in Entity entity, ref ChildOf component)
    {
        if (!_parents.TryGetValue(component.parent.Entity, out var childs))
            childs = [];
        childs.Add(component);
        if(component.parent.Entity.GetParent<P>(out var parent))
        {
            // TODO set dirty flag on each K in childs of entity if entity not P
            // if entity not P and entity is K - set dirty flag on K
        }
    }

    private void OnStopChild(in Entity entity, ref ChildOf component)
    {
        Debug.Assert(_parents.ContainsKey(component.parent.Entity));
        if(component.parent.Entity.GetParent<P>(out var parent))
        {

        }
        _parents[component.parent.Entity].Remove(component);
    }

    private void OnComponentChanged(in Entity entity, ref ChildOf component)
    {
        _world.AddOrGet<DirtyParent<P>>(entity);
    }

    private struct DirtyParent<M>();
}
