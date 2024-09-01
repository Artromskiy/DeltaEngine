using BenchmarkDotNet.Attributes;
using Collections.Pooled;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DeltaBench;

//[SimpleJob(launchCount: 5, iterationCount: 50)]
[SimpleJob(iterationCount: 50)]
[MeanColumn, StdErrorColumn, StdDevColumn, MedianColumn, MemoryDiagnoser]
public unsafe class DuplicateBench
{
    private const int Magic = 132;
    private const int N = 50;
    private const int R = 50000;
    private readonly int[][] _itemsSource = new int[R][];
    private readonly int[][] _items = new int[R][];
    private static readonly EqualityComparer<int> comparer = EqualityComparer<int>.Default;
    private static Random rnd = new(11);
    private Span<int> Items(int set) => _items[set];

    public DuplicateBench()
    {
        rnd = new(Magic);
        for (int i = 0; i < R; i++)
        {
            _items[i] = new int[N];
            for (int j = 0; j < N; j++)
                _items[i][j] = rnd.Next(0, N);
        }
    }

    [GlobalSetup]
    public void StaticSetup()
    {
        rnd = new(Magic);
        for (int i = 0; i < R; i++)
        {
            _itemsSource[i] = new int[N];
            _items[i] = new int[N];
            for (int j = 0; j < N; j++)
                _itemsSource[i][j] = rnd.Next(0, N);
        }
    }

    [IterationSetup]
    public void Setup()
    {
        for (int i = 0; i < R; i++)
            Array.Copy(_itemsSource[i], _items[i], N);
    }

    [Benchmark]
    public int InPlaceGoTo()
    {
        int count = 0;
        for (int i = 0; i < R; i++)
        {
            count += Method(Items(i));
        }
        static int Method(Span<int> items)
        {
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
        return count;
    }

    [Benchmark]
    public int InPlaceGoToTillLastCached()
    {
        int count = 0;
        for (int i = 0; i < R; i++)
        {
            count += Method(Items(i));
        }
        static int Method(Span<int> items)
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
        return count;
    }

    [Benchmark]
    public int SpanMask()
    {
        int count = 0;
        for (int i = 0; i < R; i++)
        {
            count += Method(Items(i));
        }
        static int Method(Span<int> items)
        {
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
        return count;
    }

    [SkipLocalsInit]
    [Benchmark]
    public int SpanMaskSkipInit()
    {
        int count = 0;
        for (int i = 0; i < R; i++)
        {
            count += Method(Items(i));
        }
        static int Method(Span<int> items)
        {
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
        return count;
    }

    [SkipLocalsInit]
    [Benchmark]
    public int SpanMaskSkipInit2()
    {
        int count = 0;
        for (int i = 0; i < R; i++)
        {
            count += Method(Items(i));
        }
        static int Method(Span<int> items)
        {
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
        return count;
    }

    [Benchmark]
    public int RentedMask()
    {
        int count = 0;
        for (int i = 0; i < R; i++)
        {
            count += Method(Items(i));
        }
        static int Method(Span<int> items)
        {
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
        return count;
    }

    [Benchmark]
    public int ArrayMask()
    {
        int count = 0;
        for (int i = 0; i < R; i++)
        {
            count += Method(Items(i));
        }
        static int Method(Span<int> items)
        {
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
        return count;
    }

    [Benchmark]
    public int HashSetCopy1()
    {
        int count = 0;
        for (int i = 0; i < R; i++)
        {
            count += Method(Items(i));
        }
        static int Method(Span<int> items)
        {
            HashSet<int> set = [.. items];
            int i = 0;
            foreach (var item in set)
                items[i++] = item;
            return set.Count;
        }
        return count;
    }

    [Benchmark]
    public int PooledSetCopy()
    {
        int count = 0;
        for (int i = 0; i < R; i++)
        {
            count += Method(Items(i));
        }
        static int Method(Span<int> items)
        {
            PooledSet<int> set = [.. items];
            int i = 0;
            foreach (var item in set)
                items[i++] = item;
            int count = set.Count;
            set.Dispose();
            return count;
        }
        return count;
    }
}
