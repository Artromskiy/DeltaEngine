using BenchmarkDotNet.Attributes;

namespace DesignDrivedBenchmarks
{
    public class ClassVSGuidEquality
    {
        private const int N = 10000;
        private readonly Guid simpleGuid = Guid.Empty;
        private readonly object simpleObject = new();
        private readonly Guid[] guidArray = [];
        private readonly object[] refArray = [];

        public ClassVSGuidEquality()
        {
            guidArray = new Guid[N];
            refArray = new object[N];
            for (int i = 0; i < N; i++)
            {
                guidArray[i] = Guid.NewGuid();
                refArray[i] = new();
            }
            Random.Shared.Shuffle(guidArray);
            Random.Shared.Shuffle(refArray);
        }

        [Benchmark]
        public int CheckGuidEquals()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                count += guidArray[i].Equals(simpleGuid) ? 1 : 0;
            return count;
        }

        [Benchmark]
        public int CheckGuidEqual()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                count += guidArray[i] == simpleGuid ? 1 : 0;
            return count;
        }

        [Benchmark]
        public int CheckRef()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                count += ReferenceEquals(refArray[i], simpleObject) ? 1 : 0;
            return count;
        }
    }
}
