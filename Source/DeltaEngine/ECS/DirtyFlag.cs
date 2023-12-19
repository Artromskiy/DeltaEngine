using Arch.Core;
using Arch.Core.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeltaEngine.ECS;

internal interface IDirty { }

internal struct DirtyFlag<T> { }

internal static class DirtyExtensions
{
    private static readonly Dictionary<QueryDescription, Dictionary<ComponentType, QueryDescription>> _nonDirtyLookup = [];
    private static readonly Dictionary<Type, bool> _iDirtyLookup = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InlineDirtyQuery<T, T0>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0>
    {
        AddDirty<T0>(world, description);
        world.InlineQuery<T, T0>(description, ref iForEach);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InlineDirtyQuery<T, T0, T1>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0, T1>
    {
        AddDirty<T0>(world, description);
        AddDirty<T1>(world, description);
        world.InlineQuery<T, T0, T1>(description, ref iForEach);
    }

    public static void InlineDirtyParallelQuery<T, T0, T1>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0, T1>
    {
        AddDirty<T0>(world, description);
        AddDirty<T1>(world, description);
        world.InlineParallelQuery<T, T0, T1>(description, ref iForEach);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InlinePrallelDirtyQuery<T, T0>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0>
    {
        AddDirty<T0>(world, description);
        world.InlineParallelQuery<T, T0>(description, ref iForEach);
    }

    private static void AddDirty<T>(this World world, in QueryDescription description)
    {
        if (new Impl<IDirty, T>())
        {
            var desc = GetNonDirty<T>(description);
            world.Add<DirtyFlag<T>>(desc);
        }
    }

    private static QueryDescription GetNonDirty<T>(QueryDescription description)
    {
        var cmp = Component<T>.ComponentType;
        var dirtCmp = Component<DirtyFlag<T>>.ComponentType;
        if (!_nonDirtyLookup.TryGetValue(description, out var dict))
            _nonDirtyLookup[description] = dict = [];
        if (!dict.TryGetValue(cmp, out var desc))
        {
            desc = description;
            var allIndex = Array.IndexOf(desc.All, cmp);
            if (allIndex == -1)
            {
                var newAll = new ComponentType[desc.All.Length + 1];
                desc.All.CopyTo(newAll, 0);
                newAll[^1] = cmp;
                desc.All = newAll;
            }
            var noneIndex = Array.IndexOf(desc.None, dirtCmp);
            if (noneIndex == -1)
            {
                var newNone = new ComponentType[desc.None.Length + 1];
                desc.None.CopyTo(newNone, 0);
                newNone[^1] = dirtCmp;
                desc.None = newNone;
            }
            dict[cmp] = desc;
        }
        return desc;
    }

    public readonly ref  struct Impl<Iface, T>
    {
        private static readonly bool _implements = typeof(Iface).IsAssignableFrom(typeof(T));

        public static implicit operator bool(Impl<Iface, T> _) => _implements;
    }
}