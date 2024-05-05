using Arch.Core;
using Delta.ECS.Components;
using Delta.Scripting;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Delta.ECS;

[System]
public partial struct CustomQuery
{
    [SystemCall]
    private void Update(ref Order order, ref Transform transform, ref readonly Render render)
    {
        order.order = 11;
        transform.position += Vector3.UnitY;
    }
}

public partial struct CustomQuery : ISystem
{
    public void UpdateExpanded(QueryDescription queryDescription)
    {
        var query = World.Worlds[0].Query(in queryDescription);
        foreach (ref var chunk in query)
        {
            var firstElement = chunk.GetFirst<Order, Transform, Render>();
            foreach (var entityIndex in chunk)
            {
                ref var order = ref Unsafe.Add(ref firstElement.t0, entityIndex);
                ref var transform = ref Unsafe.Add(ref firstElement.t1, entityIndex);
                ref var render = ref Unsafe.Add(ref firstElement.t2, entityIndex);
                Update(ref order, ref transform, ref render);
            }
        }
    }

    private static readonly Type[] _reads = [];
    private static readonly Type[] _muts = [];
    public readonly ReadOnlySpan<Type> Ref => new(_reads);
    public readonly ReadOnlySpan<Type> RefReadonly => new(_muts);

    void ISystem.Execute()
    {
        UpdateExpanded(new QueryDescription());
    }
}

public struct SystemContainer<T> where T : ISystem
{
    public T System;
}