using Arch.Core.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DeltaEngine.ECS;

internal interface IDirty
{
    //bool IsDirty { get; set; }
}

internal struct DirtyFlag<T> { }

internal static class DirtyExtensions
{
    //[MethodImpl(Inl)]
    //internal static void Set<T, D>(this ref D dirty, ref T property, ref T value) where D : struct, IDirty
    //{
    //    property = value;
    //    dirty.IsDirty = true;
    //}
    //internal static void Set<T, D>(this ref D dirty, ref T property, T value) where D : struct, IDirty
    //{
    //    property = value;
    //    dirty.IsDirty = true;
    //}
    //[MethodImpl(Inl)]
    //internal static void Clear<D>(this ref D dirty) where D : struct, IDirty => dirty.IsDirty = false;


    //public static bool Implements<I>(this Type source) => typeof(I).IsAssignableFrom(source);

    private static readonly Dictionary<Type, HashSet<ComponentType>> interfaceToCmpSet = [];
    private static readonly Dictionary<Type, ComponentType[]> interfaceToCmpArray = [];
    private static readonly HashSet<Type> _regTypes = [];
}