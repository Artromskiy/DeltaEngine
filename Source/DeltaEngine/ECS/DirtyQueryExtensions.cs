using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Delta.ECS.Components;
using Delta.Scripting;
using Delta.Utilities;
using System;
using System.Collections.Generic;

namespace Delta.ECS;

public static class DirtyQueryExtensions
{
    private static readonly Dictionary<QueryDescription, Dictionary<ComponentType, QueryDescription>> _nonDirtyLookup = [];
    private static readonly Dictionary<Type, (Type type, object flag)> typeToDirtyFlag = [];
    private static readonly Type DirtyFlagGeneric = typeof(DirtyFlag<>);

    [Imp(Inl)]
    public static void InlineDirtyQuery<T, T0>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0>
    {
        world.AddDirty<T0>(description);
        world.InlineQuery<T, T0>(description, ref iForEach);
    }

    [Imp(Inl)]
    public static void InlineDirtyQuery<T, T0, T1>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0, T1>
    {
        world.AddDirty<T0>(description);
        world.AddDirty<T1>(description);
        world.InlineQuery<T, T0, T1>(description, ref iForEach);
    }

    [Imp(Inl)]
    public static void InlineDirtyParallelQuery<T, T0, T1>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0, T1>
    {
        world.AddDirty<T0>(description);
        world.AddDirty<T1>(description);
        world.InlineParallelQuery<T, T0, T1>(description, ref iForEach);
    }

    [Imp(Inl)]
    public static void InlinePrallelDirtyQuery<T, T0>(this World world, in QueryDescription description, ref T iForEach)
        where T : struct, IForEach<T0>
    {
        world.AddDirty<T0>(description);
        world.InlineParallelQuery<T, T0>(description, ref iForEach);
    }

    [Imp(Inl)]
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

    public static void MarkDirty<T>(this Entity entity)
    {
        if (entity.Has<T>() && !entity.Has<DirtyFlag<T>>() && AttributeCache.HasAttribute<DirtyAttribute, T>())
            entity.Add<DirtyFlag<T>>();
    }

    [Imp(Sync)]
    public static void MarkDirty(this Entity entity, Type component)
    {
        if (!entity.Has(component) || !AttributeCache.HasAttribute<DirtyAttribute>(component))
            return;
        if (!typeToDirtyFlag.TryGetValue(component, out var typeNflag))
        {
            var type = DirtyFlagGeneric.MakeGenericType(component);
            var flag = Activator.CreateInstance(type);
            typeToDirtyFlag[component] = typeNflag = (type, flag)!;
        }
        if (!entity.Has(typeNflag.type))
            entity.Add(typeNflag.flag!);
    }
}

