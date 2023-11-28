using BenchmarkDotNet.Attributes;

namespace DesignDrivedBenchmarks
{
    public class ClassVSGuidHashSet
    {
        private const int N = 10000;
        private readonly Guid[] guidArray = [];
        private readonly object[] refArray = [];
        private readonly HashSet<Guid> _guids = [];
        private readonly HashSet<object> _refs = [];

        public ClassVSGuidHashSet()
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
            for (int i = 0; i < N / 2; i++)
            {
                _guids.Add(guidArray[i]);
                _refs.Add(refArray[i]);
            }
            Random.Shared.Shuffle(guidArray);
            Random.Shared.Shuffle(refArray);
        }


        [Benchmark]
        public int CheckGuid()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                count += _guids.Contains(guidArray[i]) ? 1 : 0;
            return count;
        }

        [Benchmark]
        public int CheckRef()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                count += _refs.Contains(refArray[i]) ? 1 : 0;
            return count;
        }
    }
}
