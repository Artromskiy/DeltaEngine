using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;

namespace DeltaBench;

//[SimpleJob(launchCount: 10)]
[MeanColumn, StdErrorColumn, StdDevColumn, MedianColumn, MemoryDiagnoser]
public class DelegateVsFuncPointer
{
    private const int N = 10000;
    private readonly int[] _items = new int[N];
    private static readonly EqualityComparer<int> comparer = EqualityComparer<int>.Default;
    private static readonly Random rnd = new(11);
    private Span<int> Items => _items;

    public DelegateVsFuncPointer()
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
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SortLambda()
    {
        DuplicateCountDelegate(Items, (x1, x2) => comparer.Equals(x1, x2));
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SortStaticLambda()
    {
        DuplicateCountDelegate(Items, static (x1, x2) => comparer.Equals(x1, x2));
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SortLocalFunction()
    {
        bool Equals(int x1, int x2) => comparer.Equals(x1, x2);
        DuplicateCountDelegate(Items, Equals);
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SortLocalStaticFunction()
    {
        static bool Equals(int x1, int x2) => comparer.Equals(x1, x2);
        DuplicateCountDelegate(Items, Equals);
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void SortStaticFunction()
    {
        DuplicateCountDelegate(Items, EqualsMethod);
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public unsafe void SortLocalFuncPointer()
    {
        static bool Equals(int x1, int x2) => comparer.Equals(x1, x2);
        DuplicateCountFuncPointer(Items, &Equals);
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public unsafe void SortFuncPointer()
    {
        DuplicateCountFuncPointer(Items, &EqualsMethod);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool EqualsMethod(int x1, int x2) => comparer.Equals(x1, x2);


    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static int DuplicateCountDelegate<T>(Span<T> items, Func<T, T, bool> comparer)
    {
        int count = items.Length;
        int duplicateCount = 0;
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < i; j++)
                if (comparer.Invoke(items[i], items[j]))
                {
                    duplicateCount++;
                    break;
                }
        }
        return duplicateCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe int DuplicateCountFuncPointer<T>(Span<T> items, delegate*<T, T, bool> comparer)
    {
        int count = items.Length;
        int duplicateCount = 0;
        for (int i = 0; i < count; i++)
        {
            for (int j = 0; j < i; j++)
                if (comparer(items[i], items[j]))
                {
                    duplicateCount++;
                    break;
                }
        }
        return duplicateCount;
    }
}
