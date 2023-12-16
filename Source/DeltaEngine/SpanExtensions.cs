using System;
using System.Collections.Generic;
using System.Numerics;
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

    public static int FindIndex<T>(this ReadOnlySpan<T> span, Predicate<T> match) where T : struct
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (match(span[i]))
                return i;
        }
        return -1;
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

    public static int FindIndex<T>(this Span<T> span, Predicate<T> match) where T : struct
    {
        ReadOnlySpan<T> ro = span;
        return FindIndex(ro, match);
    }

    public static unsafe void CopyTo<T>(this T[] sourceArray, T* destinationStackAlloc) where T : unmanaged
    {
        if (sourceArray == null || sourceArray.Length == 0)
            return;
        var bytesSize = Buffer.ByteLength(sourceArray);
        fixed (T* ptr = sourceArray)
            Buffer.MemoryCopy(ptr, destinationStackAlloc, bytesSize, bytesSize);
    }

    public static unsafe UInt128 CheckSum<T>(this Span<T> span) where T : unmanaged
    {
        var bytes = MemoryMarshal.Cast<T, byte>(span);
        UInt128 checkSum = 0;
        int size = 16;
        var count = bytes.Length / size;
        for (int i = 0; i < count; i++)
        {
            int index = i * size;
            checkSum += (UInt128)new BigInteger(bytes[index..size]);
        }
        checkSum += (UInt128)new BigInteger(bytes[(count * size)..]);
        return checkSum;
    }

    public static Dictionary<TKey, List<TValue>> GetGroup<TKey, TValue>(this TValue[] values, Func<TValue, TKey> keySelector) where TKey : notnull
    {
        Dictionary<TKey, List<TValue>> groups = new();
        foreach (var item in values)
        {
            var key = keySelector(item);
            if (!groups.TryGetValue(key, out var list))
                groups[key] = list = new();
            list.Add(item);
        }
        return groups;
    }
    public static List<List<TValue>> GetGroupList<TKey, TValue>(this TValue[] values, Func<TValue, TKey> keySelector) where TKey : notnull
    {
        Dictionary<TKey, List<TValue>> groups = new();
        foreach (var item in values)
        {
            var key = keySelector(item);
            if (!groups.TryGetValue(key, out var list))
                groups[key] = list = new();
            list.Add(item);
        }
        List<List<TValue>> result = new();
        foreach (var item in groups)
            result.Add(item.Value);
        return result;
    }


    public static Dictionary<TKey, List<TValue>> GetGroup<TKey, TValue>(this List<TValue> values, Func<TValue, TKey> keySelector) where TKey : notnull
    {
        Dictionary<TKey, List<TValue>> groups = new();
        foreach (var item in values)
        {
            var key = keySelector(item);
            if (!groups.TryGetValue(key, out var list))
                groups[key] = list = new();
            list.Add(item);
        }
        return groups;
    }
}
