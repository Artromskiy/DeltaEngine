using BenchmarkDotNet.Attributes;
using Delta.Utilities;
using Schedulers;

namespace DeltaBench;

[SimpleJob(launchCount: 1, warmupCount: 50, iterationCount: 200)]
[MeanColumn, StdErrorColumn, StdDevColumn, MedianColumn, MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class ByteArrayCopyBench
{
    private const int Magic = 132;
    private const int C = 1000;
    private const int W = 1920;
    private const int H = 1200;
    private const int P = 4;
    private const int S = W * H * P;

    private static JobScheduler _jobScheduler;

    [ParamsSource(nameof(ThreadsCount))]
    public int Threads;
    [Params(true, false)]
    public bool UseScheduler;
    public static IEnumerable<int> ThreadsCount => Enumerable.Range(1, 2);

    private readonly Memory<byte> _destination = new byte[S];
    private readonly Memory<byte> _items = new byte[S];
    private static Random rnd = new(11);

    public ByteArrayCopyBench()
    {
        rnd = new(Magic);
        rnd.NextBytes(_items.Span);
    }

    [GlobalSetup]
    public void StaticSetup()
    {
        rnd = new(Magic);
        rnd.NextBytes(_items.Span);
        if (UseScheduler)
        {
            JobScheduler.Config jobConfig = new()
            {
                ThreadPrefixName = "Arch.Multithreading",
                ThreadCount = 0,
                MaxExpectedConcurrentJobs = 64,
                StrictAllocationMode = false,
            };
            _jobScheduler = new(jobConfig);
        }
    }

    [IterationSetup]
    public void Setup()
    {
        _destination.Span.Clear();
    }

    [Benchmark]
    public int Copy()
    {
        if (UseScheduler)
            for (int i = 0; i < C; i++)
                CopyToParallelScheduled(_items, _destination, Threads);
        else
        {
            if (Threads == 1)
                for (int i = 0; i < C; i++)
                    _items.CopyTo(_destination);
            else
                for (int i = 0; i < C; i++)
                    _items.CopyToParallel(_destination, Threads);
        }
        return rnd.Next();
    }


    public static unsafe void CopyToParallelScheduled<T>(Memory<T> source, Memory<T> destination, int threads)
    {
        if (source.Length != destination.Length)
            throw new Exception();

        int cores = int.Min(Environment.ProcessorCount, threads);
        int segmentsCount = cores;
        var segmentSize = source.Length / segmentsCount;

        Span<JobHandle> handles = stackalloc JobHandle[segmentsCount];
        for (int i = 0; i < segmentsCount; i++)
            handles[i] = _jobScheduler.Schedule(new DataCopy<T>(i, segmentSize, source, destination));
        _jobScheduler.Flush();
        JobHandle.CompleteAll(handles);

        int tailBegin = segmentSize * segmentsCount;
        source = source[tailBegin..];
        destination = destination[tailBegin..];
        source.CopyTo(destination);
    }

    private readonly struct DataCopy<T>(int id, int segmentSize, Memory<T> source, Memory<T> destination) : IJob
    {
        public readonly Memory<T> source = source;
        public readonly Memory<T> destination = destination;

        public readonly unsafe void Execute()
        {
            var sourceSpan = source.Slice(segmentSize * id, segmentSize);
            var destinationSpan = destination.Slice(segmentSize * id, segmentSize);
            sourceSpan.CopyTo(destinationSpan);
        }
    }
}
