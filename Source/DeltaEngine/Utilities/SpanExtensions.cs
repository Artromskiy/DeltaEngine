using CommunityToolkit.HighPerformance.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Delta.Utilities;

public static class SpanExtensions
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

    public static unsafe void CopyToParallel<T>(this Memory<T> source, Memory<T> destination)
    {
        CopyToParallel(source, destination, Environment.ProcessorCount);
    }

    public static unsafe void CopyToParallel<T>(this Memory<T> source, Memory<T> destination, int threads)
    {
        if (source.Length != destination.Length)
            throw new Exception();

        int cores = int.Min(Environment.ProcessorCount, threads);

        int segmentsCount = cores;
        var segmentSize = source.Length / segmentsCount;

        DataCopy<T> dataCopy = new(segmentSize, source, destination);
        ParallelHelper.For(0, segmentsCount, dataCopy);

        int tailBegin = segmentSize * segmentsCount;
        source = source[tailBegin..];
        destination = destination[tailBegin..];
        source.CopyTo(destination);
    }

    private readonly struct DataCopy<T>(int segmentSize, Memory<T> source, Memory<T> destination) : IAction
    {
        public readonly Memory<T> source = source;
        public readonly Memory<T> destination = destination;

        [Imp(Inl)]
        public readonly unsafe void Invoke(int i)
        {
            var sourceSpan = source.Slice(segmentSize * i, segmentSize);
            var destinationSpan = destination.Slice(segmentSize * i, segmentSize);
            sourceSpan.CopyTo(destinationSpan);
        }
    }

    #region Distinct
    public static int Distinct<T>(this Span<T> items)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int count = items.Length;
        int lastNonDuplicate = 0;
        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            for (int j = 0; j < lastNonDuplicate; j++)
                if (comparer.Equals(item, items[j]))
                    goto end;
            items[lastNonDuplicate++] = item;
        end:;
        }
        return lastNonDuplicate;
    }

    public static int Distinct<T>(this Span<T> items, IEqualityComparer<T> comparer)
    {
        int count = items.Length;
        int lastNonDuplicate = 0;
        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            for (int j = 0; j < lastNonDuplicate; j++)
                if (comparer.Equals(item, items[j]))
                    goto end;
            items[lastNonDuplicate++] = item;
        end:;
        }
        return lastNonDuplicate;
    }
    #endregion

    #region CountRepetitions

    public static int CountRepetitions<T>(this ReadOnlySpan<T> items, Span<(T key, int count)> repeatCount)
    {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        int count = items.Length;
        int lastNonDuplicate = 0;
        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            for (int j = 0; j < lastNonDuplicate; j++)
                if (comparer.Equals(item, repeatCount[j].key))
                {
                    repeatCount[j].count++;
                    goto end;
                }
            repeatCount[lastNonDuplicate++] = (item, 1);
        end:;
        }
        return lastNonDuplicate;
    }

    public static int CountRepetitions<T>(this Span<T> items, Span<(T key, int count)> repeatCount)
    {
        return CountRepetitions((ReadOnlySpan<T>)items, repeatCount);
    }

    #endregion
}
