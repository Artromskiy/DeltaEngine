using Arch.Core;
using System;
using System.Runtime.CompilerServices;

namespace Delta.ECS;
internal class VariadicGenerator
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class RefTuple : Attribute { }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class ReadonlyRefTuple : Attribute { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InlineQuery<T, T0, T1>(in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0, T1>
    {
        var query = World.Worlds[0].Query(in description);
        foreach (ref var chunk in query)
        {
            var refs = chunk.GetFirst<T0, T1>();
            foreach (var entityIndex in chunk)
            {
                ref var t0Component = ref Unsafe.Add(ref refs.t0, entityIndex);
                ref var t1Component = ref Unsafe.Add(ref refs.t1, entityIndex);
                iForEach.Update(ref t0Component, ref t1Component);
            }
        }
    }
}