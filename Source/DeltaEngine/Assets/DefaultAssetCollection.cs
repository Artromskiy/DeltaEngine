using Delta.Runtime;
using Delta.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Delta.Assets;
internal class DefaultAssetCollection<T> : IAssetCollection<T> where T : class, IAsset
{
    private const string MetaEnding = ".meta";

    private readonly Dictionary<Guid, GuidAssetData> _assetsData = [];
    private readonly Dictionary<Guid, GuidAssetData> _runtimeAssetsData = [];

    public string GetPath(GuidAsset<T> guidAsset)
    {
        if (!_assetsData.TryGetValue(guidAsset.guid, out var result))
            _runtimeAssetsData.TryGetValue(guidAsset.guid, out result);
        return result?.path ?? string.Empty;
    }
    public string GetName(GuidAsset<T> guidAsset) => Path.GetFileNameWithoutExtension(GetPath(guidAsset));

    public T GetAsset(GuidAsset<T> guidAsset)
    {
        var dictionary = _assetsData.ContainsKey(guidAsset.guid) ? _assetsData : _runtimeAssetsData;
        if (!dictionary.TryGetValue(guidAsset.guid, out var data))
            throw new FileNotFoundException();

        if (!data.assetRef.TryGetTarget(out var asset))
            data.assetRef.SetTarget(asset = LoadAsset(data.path));

        return asset;
    }

    [Imp(Sync)]
    public GuidAsset<T> CreateAsset(T asset, string name)
    {
        var resourceDirectory = IRuntimeContext.Current.ProjectPath.ResourcesDirectory;
        string path = FileHelper.CreateIndexedFile(resourceDirectory, name);

        var guid = Guid.NewGuid();
        var meta = new Meta(guid, 0);

        SaveAsset(asset, path);
        Serialization.Serialize($"{path}{MetaEnding}", meta);

        _assetsData.Add(guid, new GuidAssetData(asset, path));

        return new GuidAsset<T>(guid);
    }

    [Imp(Sync)]
    public GuidAsset<T> CreateRuntimeAsset(T asset, string? name)
    {
        var guid = Guid.NewGuid();
        name ??= guid.ToString();
        var tempDirectory = IRuntimeContext.Current.ProjectPath.TempDirectory;
        string path = FileHelper.CreateIndexedFile(tempDirectory, name);

        SaveAsset(asset, path);

        _runtimeAssetsData.Add(guid, new GuidAssetData(asset, path));

        return new GuidAsset<T>(guid);
    }

    public virtual void SaveAsset(T asset, string path) => Serialization.Serialize(path, asset);
    public virtual T LoadAsset(string path) => Serialization.Deserialize<T>(path);

    public GuidAsset<T>[] GetAssets()
    {
        GuidAsset<T>[] guidAssets = new GuidAsset<T>[_assetsData.Count];
        int index = 0;
        foreach (var item in _assetsData)
            guidAssets[index++] = new GuidAsset<T>(item.Key);
        return guidAssets;
    }

    public GuidAsset<T>[] GetRuntimeAssets()
    {
        GuidAsset<T>[] guidAssets = new GuidAsset<T>[_runtimeAssetsData.Count];
        int index = 0;
        foreach (var item in _runtimeAssetsData)
            guidAssets[index++] = new GuidAsset<T>(item.Key);
        return guidAssets;
    }

    public GuidAsset<T>[] GetAllAssets()
    {
        GuidAsset<T>[] guidAssets = new GuidAsset<T>[_assetsData.Count + _runtimeAssetsData.Count];
        int index = 0;
        foreach (var item in _assetsData)
            guidAssets[index++] = new GuidAsset<T>(item.Key);
        foreach (var item in _runtimeAssetsData)
            guidAssets[index++] = new GuidAsset<T>(item.Key);
        return guidAssets;
    }

    public int GetAssetsCount() => _assetsData.Count;
    public int GetRuntimeAssetsCount() => _runtimeAssetsData.Count;
    public int GetAllAssetsCount() => _assetsData.Count + _runtimeAssetsData.Count;

    private class GuidAssetData
    {
        public readonly WeakReference<T?> assetRef;
        public readonly string path;

        public GuidAssetData(T asset, string path)
        {
            assetRef = new WeakReference<T?>(asset);
            this.path = path;
        }
    }
}
