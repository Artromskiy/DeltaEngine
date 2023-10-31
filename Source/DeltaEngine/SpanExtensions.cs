﻿using System;
using System.Runtime.InteropServices;

namespace DeltaEngine;

internal static class SpanExtensions
{
    public static bool Exist<T>(this ReadOnlySpan<T> span, Predicate<T> match, out T result) where T : struct
    {
        result = default;
        foreach (var item in span)
            if (match(item))
            {
                result = item;
                return true;
            }
        return false;
    }

    public static bool Exist<T>(this ReadOnlySpan<T> span, Predicate<T> match) where T : struct
    {
        foreach (var item in span)
            if (match(item))
                return true;
        return false;
    }

    public static T Find<T>(this ReadOnlySpan<T> span, Predicate<T> match) where T : struct
    {
        foreach (var item in span)
            if (match(item))
                return item;
        return default;
    }

    public static bool Exist<T>(this Span<T> span, Predicate<T> match) where T : struct
    {
        ReadOnlySpan<T> ro = span;
        return Exist(ro, match);
    }
    public static bool Exist<T>(this Span<T> span, Predicate<T> match, out T result) where T : struct
    {
        ReadOnlySpan<T> ro = span;
        return Exist(ro, match, out result);
    }

    public static T Find<T>(this Span<T> span, Predicate<T> match) where T : struct
    {
        ReadOnlySpan<T> ro = span;
        return Find(ro, match);
    }

    public static unsafe void CopyTo<T>(this T[] sourceArray, T* destinationStackAlloc) where T : unmanaged
    {
        if (sourceArray == null || sourceArray.Length == 0)
            return;
        var bytesSize = Buffer.ByteLength(sourceArray);
        fixed (T* ptr = sourceArray)
            Buffer.MemoryCopy(ptr, destinationStackAlloc, bytesSize, bytesSize);
    }
}
