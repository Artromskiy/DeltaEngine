using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DeltaEngine.Files;

internal class RuntimeAssetCollection
{
    private readonly ConditionalWeakTable<object, IAsset> _runtimeAssetCollection = new();

    public T LoadAsset<T>(GuidAsset<T> guid) where T : IAsset
    {
        Debug.Assert(guid._runtimeRef != null);
        _runtimeAssetCollection.TryGetValue(guid._runtimeRef, out var result);
        Debug.Assert(result != default);
        return (T)result;
    }
    public GuidAsset<T> CreateAsset<T>(T asset) where T : IAsset
    {
        var guidAsset = new GuidAsset<T>(Guid.NewGuid(), true);
        _runtimeAssetCollection.Add(guidAsset._runtimeRef!, asset);
        return guidAsset;
    }
}
