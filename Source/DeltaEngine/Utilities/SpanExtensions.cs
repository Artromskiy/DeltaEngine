using System;
using System.Collections.Generic;

namespace Delta.Utilities;

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

    public static unsafe bool Exist<T>(this ReadOnlySpan<T> span, delegate*<T, bool> match) where T : struct
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

    public static int FindIndex<T>(this ReadOnlySpan<T> span, Predicate<T> match) where T : struct
    {
        for (int i = 0; i < span.Length; i++)
            if (match(span[i]))
                return i;
        return -1;
    }

    public static bool Exist<T>(this Span<T> span, Predicate<T> match) where T : struct
    {
        ReadOnlySpan<T> ro = span;
        return ro.Exist(match);
    }

    public static unsafe bool Exist<T>(this Span<T> span, delegate*<T, bool> match) where T : struct
    {
        ReadOnlySpan<T> ro = span;
        return ro.Exist(match);
    }

    public static bool Exist<T>(this Span<T> span, Predicate<T> match, out T result) where T : struct
    {
        ReadOnlySpan<T> ro = span;
        return ro.Exist(match, out result);
    }

    public static T Find<T>(this Span<T> span, Predicate<T> match) where T : struct
    {
        ReadOnlySpan<T> ro = span;
        return ro.Find(match);
    }

    public static int FindIndex<T>(this Span<T> span, Predicate<T> match) where T : struct
    {
        ReadOnlySpan<T> ro = span;
        return ro.FindIndex(match);
    }

    public static unsafe void CopyTo<T>(this T[] array, T* pointer) where T : unmanaged
    {
        Span<T> span = new(pointer, array.Length);
        array.CopyTo(span);
    }

    public static unsafe void CopyTo<T>(this Span<T> array, T* pointer) where T : unmanaged
    {
        Span<T> span = new(pointer, array.Length);
        array.CopyTo(span);
    }

    public static unsafe void CopyTo<T>(this ReadOnlySpan<T> array, T* pointer) where T : unmanaged
    {
        Span<T> span = new(pointer, array.Length);
        array.CopyTo(span);
    }

    public static int Distinct<T>(this Span<T> items)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int count = items.Length;
        int lastNonDuplicate = 0;
        Span<bool> duplicateMask = stackalloc bool[count];
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < i; j++)
                if (!duplicateMask[j] && comparer.Equals(items[i], items[j]) && (duplicateMask[i] = true))
                    break;
        }
        for (int i = 0; i < count; i++)
            if (!duplicateMask[i])
                items[lastNonDuplicate++] = items[i];
        return lastNonDuplicate;
    }

}
