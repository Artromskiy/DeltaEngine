﻿using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace DeltaBench
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IConfig? config = null;
#if DEBUG
            config = new DebugInProcessConfig();
#endif
            var summary = BenchmarkRunner.Run<RefGetBench>(config);
            Console.ReadKey();
        }
    }
}