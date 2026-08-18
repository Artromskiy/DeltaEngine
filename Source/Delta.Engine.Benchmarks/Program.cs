using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Delta.Engine.Benchmarks;

internal class Program
{
    private static void Main(string[] args)
    {
        IConfig? config = null;
#if DEBUG
        config = new DebugInProcessConfig();
#endif
        var summary = BenchmarkRunner.Run<MatrixBench>(config);
        Console.ReadKey();
    }
}
