using BenchmarkDotNet.Attributes;

namespace DesignDrivedBenchmarks
{
    public class ClassVSGuidDictionary
    {
        private const int N = 10000;
        private readonly Guid[] guidArray = [];
        private readonly object[] refArray;
        private readonly Dictionary<Guid, int> _guids = [];
        private readonly Dictionary<object, int> _refs = [];

        public ClassVSGuidDictionary()
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
                _guids.Add(guidArray[i], Random.Shared.Next());
                _refs.Add(refArray[i], Random.Shared.Next());
            }
            Random.Shared.Shuffle(guidArray);
            Random.Shared.Shuffle(refArray);
        }

        [Benchmark]
        public int CheckGuid()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                if (_guids.TryGetValue(guidArray[i], out int res))
                    count += res;
            return count;
        }

        [Benchmark]
        public int CheckRef()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                if (_refs.TryGetValue(refArray[i], out int res))
                    count += res;
            return count;
        }
    }
}
