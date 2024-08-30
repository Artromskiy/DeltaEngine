using BenchmarkDotNet.Attributes;
using Collections.Pooled;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DeltaBench;

[SimpleJob(launchCount: 10)]
[MeanColumn, StdErrorColumn, StdDevColumn, MedianColumn, MemoryDiagnoser]
public unsafe class DuplicateBench
{
    private const int N = 50;
    private readonly int[] _items = new int[N];
    private static readonly EqualityComparer<int> comparer = EqualityComparer<int>.Default;
    private static readonly Random rnd = new(11);
    private Span<int> Items => _items;

    public DuplicateBench()
    {
        for (int i = 0; i < N; i++)
            _items[i] = rnd.Next(0, N);
    }

    [IterationSetup]
    public void Setup()
    {
        for (int i = 0; i < N; i++)
            _items[i] = rnd.Next(0, N);
    }

    [Benchmark]
    public int InPlaceGoTo()
    {
        var items = Items;
        int count = items.Length;
        int lastNonDuplicate = 0;
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < i; j++)
                if (comparer.Equals(items[i], items[j]))
                    goto end;
            items[lastNonDuplicate++] = items[i];
        end:;
        }
        return lastNonDuplicate;
    }

    [Benchmark]
    public int SpanMask()
    {
        var items = Items;
        int count = items.Length;
        Span<bool> duplicateMask = stackalloc bool[count];
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < i; j++)
                if (!duplicateMask[j] && comparer.Equals(items[i], items[j]) && (duplicateMask[i] = true))
                    break;
        }
        int lastNonDuplicate = 0;
        for (int i = 0; i < count; i++)
            if (!duplicateMask[i])
                items[lastNonDuplicate++] = items[i];
        return lastNonDuplicate;
    }

    [Benchmark]
    [SkipLocalsInit]
    public int SpanMaskSkipInit()
    {
        var items = Items;
        int count = items.Length;
        Span<bool> duplicateMask = stackalloc bool[count];
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < i; j++)
                if (!duplicateMask[j] && comparer.Equals(items[i], items[j]) && (duplicateMask[i] = true))
                    goto end;
            duplicateMask[i] = false;
        end:;
        }
        int lastNonDuplicate = 0;
        for (int i = 0; i < count; i++)
            if (!duplicateMask[i])
                items[lastNonDuplicate++] = items[i];
        return lastNonDuplicate;
    }

    [Benchmark]
    [SkipLocalsInit]
    public int SpanMaskSkipInit2()
    {
        var items = Items;
        int count = items.Length;
        Span<bool> duplicateMask = stackalloc bool[count];
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < i; j++)
                if (!duplicateMask[j] && comparer.Equals(items[i], items[j]))
                {
                    duplicateMask[i] = true;
                    goto end;
                }
            duplicateMask[i] = false;
        end:;
        }
        int lastNonDuplicate = 0;
        for (int i = 0; i < count; i++)
            if (!duplicateMask[i])
                items[lastNonDuplicate++] = items[i];
        return lastNonDuplicate;
    }

    [Benchmark]
    public int RentedMask()
    {
        var items = Items;
        int count = items.Length;
        bool[] duplicateMask = ArrayPool<bool>.Shared.Rent(count);
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < i; j++)
                if (!duplicateMask[j] && comparer.Equals(items[i], items[j]) && (duplicateMask[i] = true))
                    break;
        }
        int lastNonDuplicate = 0;
        for (int i = 0; i < count; i++)
            if (!duplicateMask[i])
                items[lastNonDuplicate++] = items[i];
        ArrayPool<bool>.Shared.Return(duplicateMask);
        return lastNonDuplicate;
    }

    [Benchmark]
    public int ArrayMask()
    {
        var items = Items;
        int count = items.Length;
        bool[] duplicateMask = new bool[count];
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < i; j++)
                if (!duplicateMask[j] && comparer.Equals(items[i], items[j]) && (duplicateMask[i] = true))
                    break;
        }
        int lastNonDuplicate = 0;
        for (int i = 0; i < count; i++)
            if (!duplicateMask[i])
                items[lastNonDuplicate++] = items[i];
        return lastNonDuplicate;
    }

    [Benchmark]
    public int HashSetCopy1()
    {
        var items = Items;
        HashSet<int> set = [.. items];
        int i = 0;
        foreach (var item in set)
            items[i++] = item;
        return set.Count;
    }

    [Benchmark]
    public int PooledSetCopy()
    {
        var items = Items;
        PooledSet<int> set = [.. items];
        int i = 0;
        foreach (var item in set)
            items[i++] = item;
        int count = set.Count;
        set.Dispose();
        return count;
    }
}
