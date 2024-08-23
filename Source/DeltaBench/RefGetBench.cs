using BenchmarkDotNet.Attributes;
using System.Runtime.CompilerServices;

namespace DeltaBench
{
    public class RefGetBench
    {
        public struct ContainerGetAgr
        {
            public float Value;
            public float Value1
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                readonly get => Value;
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                set => Value = value;
            }
            public float Value2
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                readonly get => Value1;
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                set => Value1 = value;
            }
            public float Value3
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                readonly get => Value2;
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                set => Value2 = value;
            }
        }

        public struct ContainerGet
        {
            public float Value;
            public float Value1
            {
                readonly get => Value;
                set => Value = value;
            }
            public float Value2
            {
                readonly get => Value1;
                set => Value1 = value;
            }
            public float Value3
            {
                readonly get => Value2;
                set => Value2 = value;
            }
        }

        private ContainerGet g0;
        private ContainerGet g1;
        private ContainerGet g2;
        private ContainerGet g3;
        private ContainerGet ga0;
        private ContainerGet ga1;
        private ContainerGet ga2;
        private ContainerGet ga3;


        [GlobalSetup]
        public void Setup()
        {
#if DEBUG
            //System.Diagnostics.Debugger.Launch();
#endif
        }

        [Benchmark(Baseline = true)]
        public float Bench0() => g0.Value += 1;
        [Benchmark]
        public float Bench1() => g1.Value1 += 1;
        [Benchmark]
        public float Bench2() => g2.Value2 += 1;
        [Benchmark]
        public float Bench3() => g3.Value3 += 1;

        [Benchmark]
        public float BenchAgr0() => ga0.Value += 1;
        [Benchmark]
        public float BenchAgr1() => ga1.Value1 += 1;
        [Benchmark]
        public float BenchAgr2() => ga2.Value2 += 1;
        [Benchmark]
        public float BenchAgr3() => ga3.Value3 += 1;
    }
}
