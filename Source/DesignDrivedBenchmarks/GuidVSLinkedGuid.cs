using BenchmarkDotNet.Attributes;

namespace DesignDrivedBenchmarks
{
    public class GuidVSLinkedGuid<L>
    {
        private const int N = 10000;
        private readonly GuidAssetSimple<L>[] _simpleAsset = [];
        private readonly GuidAssetLinked<L>[] _linkedAsset = [];


        private readonly Dictionary<GuidAssetSimple<L>, int> _simpleDict = [];
        private readonly Dictionary<GuidAssetLinked<L>, int> _linkedDict = [];

        private readonly HashSet<GuidAssetSimple<L>> _simpleSet = [];
        private readonly HashSet<GuidAssetLinked<L>> _linkedSet = [];


        public GuidVSLinkedGuid()
        {
            _simpleAsset = new GuidAssetSimple<L>[N];
            _linkedAsset = new GuidAssetLinked<L>[N];
            for (int i = 0; i < N; i++)
            {
                _simpleAsset[i] = new(Guid.NewGuid());
                _linkedAsset[i] = new(Guid.NewGuid(), i % 2 == 0);
            }
            Random.Shared.Shuffle(_simpleAsset);
            Random.Shared.Shuffle(_linkedAsset);

            for (int i = 0; i < N / 2; i++)
            {
                _simpleDict.Add(_simpleAsset[i], Random.Shared.Next());
                _linkedDict.Add(_linkedAsset[i], Random.Shared.Next());
            }
            Random.Shared.Shuffle(_linkedAsset);
            Random.Shared.Shuffle(_simpleAsset);

            for (int i = 0; i < N / 2; i++)
            {
                _simpleSet.Add(_simpleAsset[i]);
                _linkedSet.Add(_linkedAsset[i]);
            }
            Random.Shared.Shuffle(_linkedAsset);
            Random.Shared.Shuffle(_simpleAsset);
        }

        [Benchmark]
        public int DictionarySimpleAsset()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                if (_simpleDict.TryGetValue(_simpleAsset[i], out int res))
                    count += res;
            return count;
        }

        [Benchmark]
        public int DictionaryLinkedAsset()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                if (_linkedDict.TryGetValue(_linkedAsset[i], out int res))
                    count += res;
            return count;
        }

        [Benchmark]
        public int HashSetSimpleAsset()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                count += _simpleSet.Contains(_simpleAsset[i]) ? 1 : 0;
            return count;
        }

        [Benchmark]
        public int HashSetLinkedAsset()
        {
            int count = 0;
            for (int i = 0; i < N; i++)
                count += _linkedSet.Contains(_linkedAsset[i]) ? 1 : 0;
            return count;
        }


        private readonly struct GuidAssetSimple<T> : IEquatable<GuidAssetSimple<T>>
        {
            public readonly Guid guid;

            public GuidAssetSimple(Guid g)
            {
                guid = g;
            }

            public override bool Equals(object? obj) => obj is GuidAssetSimple<T> asset && asset.Equals(this);
            public bool Equals(GuidAssetSimple<T> other) => guid.Equals(other.guid);
            public override int GetHashCode() => guid.GetHashCode();
        }

        private readonly struct GuidAssetLinked<T> : IEquatable<GuidAssetLinked<T>>
        {
            public readonly Guid guid;
            private readonly object? _refLink;
            private readonly bool RuntimeAsset => _refLink != null;

            public GuidAssetLinked(Guid g, bool runtime = false)
            {
                guid = g;
                _refLink = runtime ? new() : null;
            }

            public override bool Equals(object? obj) => obj is GuidAssetLinked<T> asset && asset.Equals(this);
            public bool Equals(GuidAssetLinked<T> other) => RuntimeAsset ? ReferenceEquals(_refLink, other._refLink) : guid.Equals(other.guid);
            public override int GetHashCode() => guid.GetHashCode();
        }
    }
}
