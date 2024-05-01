using Arch.Core;
using Arch.Core.Utils;
using Delta.ECS.Components;
using Delta.Scripting;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Delta.ECS;

internal static class DirtyQueryExtensions
{
    private static readonly Dictionary<QueryDescription, Dictionary<ComponentType, QueryDescription>> _nonDirtyLookup = [];

    [MethodImpl(Inl)]
    public static void InlineDirtyQuery<T, T0>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0>
    {
        world.AddDirty<T0>(description);
        world.InlineQuery<T, T0>(description, ref iForEach);
    }

    [MethodImpl(Inl)]
    public static void InlineDirtyQuery<T, T0, T1>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0, T1>
    {
        world.AddDirty<T0>(description);
        world.AddDirty<T1>(description);
        world.InlineQuery<T, T0, T1>(description, ref iForEach);
    }

    [MethodImpl(Inl)]
    public static void InlineDirtyParallelQuery<T, T0, T1>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0, T1>
    {
        world.AddDirty<T0>(description);
        world.AddDirty<T1>(description);
        world.InlineParallelQuery<T, T0, T1>(description, ref iForEach);
    }

    [MethodImpl(Inl)]
    public static void InlinePrallelDirtyQuery<T, T0>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0>
    {
        world.AddDirty<T0>(description);
        world.InlineParallelQuery<T, T0>(description, ref iForEach);
    }

    [MethodImpl(Inl)]
    private static void AddDirty<T>(this World world, in QueryDescription description)
    {
        if (AttributeCache.HasAttribute<DirtyAttribute, T>())
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
}

