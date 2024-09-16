using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace DeltaBench;

internal class Program
{
    private static void Main(string[] args)
    {
        IConfig? config = null;
#if DEBUG
        config = new DebugInProcessConfig();
#endif
        var summary = BenchmarkRunner.Run<ByteArrayCopyBench>(config);
        Console.ReadKey();
    }
}