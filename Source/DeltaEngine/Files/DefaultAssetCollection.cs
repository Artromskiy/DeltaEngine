using Delta.Runtime;
using System;
using System.Collections.Generic;

namespace Delta.Files;
internal class DefaultAssetCollection<T> : IAssetCollection<T> where T : class, IAsset
{
    private readonly Dictionary<Guid, WeakReference<T?>> _guidToAssetRef = [];

    public T LoadAsset(GuidAsset<T> guidAsset)
    {
        if (!_guidToAssetRef.TryGetValue(guidAsset.guid, out var reference))
            _guidToAssetRef[guidAsset.guid] = reference = new(null);
        if (!reference.TryGetTarget(out var asset))
            reference.SetTarget(asset = LoadAsset(guidAsset.guid));
        return asset;
    }

    private static T LoadAsset(Guid guid)
    {
        var path = IRuntimeContext.Current.AssetImporter.GetPath(guid);
        return Serialization.Deserialize<T>(path);
    }

    public List<GuidAsset<T>> GetAssets()
    {
        List<GuidAsset<T>> guidAssets = [];
        foreach (var item in _guidToAssetRef)
            guidAssets.Add(new GuidAsset<T>(item.Key));
        return guidAssets;
    }
}
