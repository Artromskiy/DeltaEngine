﻿using System.Runtime.CompilerServices;

namespace DeltaEngine.ECS;

internal interface IDirty
{
    bool IsDirty { get; set; }
}

internal interface IDirty<T> : IDirty where T : IDirty { }

internal static class DirtyExtensions
{
    [MethodImpl(Inl)]
    internal static void Set<T, D>(this ref D dirty, ref T property, ref T value) where D : struct, IDirty
    {
        dirty.IsDirty = true;
        property = value;
    }
    [MethodImpl(Inl)]
    internal static void Clear<D>(this ref D dirty) where D : struct, IDirty => dirty.IsDirty = false;
}