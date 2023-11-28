using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;

namespace DesignDrivedBenchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var config = DefaultConfig.Instance.AddHardwareCounters
            (
                HardwareCounter.CacheMisses,
                HardwareCounter.BranchMispredictions,
                HardwareCounter.BranchInstructions
            );

            BenchmarkRunner.Run<ClassVSGuidEquality>(config);
            BenchmarkRunner.Run<ClassVSGuidDictionary>(config);
            BenchmarkRunner.Run<ClassVSGuidHashSet>(config);

            BenchmarkRunner.Run<GuidVSLinkedGuid<int>>(config);
            BenchmarkRunner.Run<GuidVSLinkedGuid<object>>(config);

            Console.ReadLine();
        }
    }
}
