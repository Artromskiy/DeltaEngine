using Delta.Runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace Delta.Files;
internal class DefaultAssetCollection<T> : IAssetCollection<T> where T : class, IAsset
{
    private const string MetaEnding = ".meta";
    private readonly Dictionary<Guid, string> _assetPaths = [];
    private readonly Dictionary<string, Guid> _pathToGuid = [];

    private readonly Dictionary<Guid, string> _tempAssetPaths = [];
    private readonly Dictionary<string, Guid> _tempPathToGuid = [];

    private readonly Dictionary<Guid, WeakReference<T?>> _guidToAsset = [];
    private readonly Dictionary<Guid, WeakReference<T?>> _tempGuidToAsset = [];

    public T GetAsset(string path) => GetAsset(PathToGuidAsset(path));
    public T GetAsset(GuidAsset<T> guidAsset)
    {
        var guidToAsset = _guidToAsset.ContainsKey(guidAsset.guid) ? _guidToAsset : _tempGuidToAsset;
        if (!guidToAsset.TryGetValue(guidAsset.guid, out var reference))
            guidToAsset[guidAsset.guid] = reference = new(null);
        if (!reference.TryGetTarget(out var asset))
            reference.SetTarget(asset = LoadAsset(GetPath(guidAsset.guid)));
        return asset;
    }

    public GuidAsset<T> CreateAsset(string name, T asset)
    {
        var resourceDirectory = IRuntimeContext.Current.ProjectPath.ResourcesDirectory;
        string path = FileHelper.CreateIndexedFile(resourceDirectory, name);

        var guid = Guid.NewGuid();
        var meta = new Meta(guid, 0);

        if (_pathToGuid.ContainsKey(path))
            throw new FileLoadException();

        SaveAsset(asset, path);
        Serialization.Serialize($"{path}{MetaEnding}", meta);

        _assetPaths.Add(guid, path);
        _pathToGuid.Add(path, guid);

        if (!_guidToAsset.TryGetValue(guid, out var reference))
            _guidToAsset[guid] = reference = new(null);
        reference.SetTarget(asset);

        return new GuidAsset<T>(guid);
    }

    public GuidAsset<T> CreateRuntimeAsset(T asset)
    {
        var guid = Guid.NewGuid();
        var tempDirectory = IRuntimeContext.Current.ProjectPath.TempDirectory;
        string path = FileHelper.CreateIndexedFile(tempDirectory, guid);
        if (_tempPathToGuid.ContainsKey(path))
            throw new FileLoadException();

        SaveAsset(asset, path);

        _tempAssetPaths.Add(guid, path);
        _tempPathToGuid.Add(path, guid);

        if (!_tempGuidToAsset.TryGetValue(guid, out var reference))
            _tempGuidToAsset[guid] = reference = new(null);
        reference.SetTarget(asset);

        return new GuidAsset<T>(guid);
    }

    public string GetPath(Guid guid)
    {
        if (!_assetPaths.TryGetValue(guid, out string? result))
            _tempAssetPaths.TryGetValue(guid, out result);
        return result!;
    }

    private GuidAsset<T> PathToGuidAsset(string path)
    {
        if (!_pathToGuid.TryGetValue(path, out Guid result))
            _tempPathToGuid.TryGetValue(path, out result);
        return new(result);
    }

    protected virtual void SaveAsset(T asset, string path)
    {
        Serialization.Serialize(path, asset);
    }

    protected virtual T LoadAsset(string path)
    {
        return Serialization.Deserialize<T>(path);
    }

    public List<GuidAsset<T>> GetAssets()
    {
        List<GuidAsset<T>> guidAssets = [];
        foreach (var item in _guidToAsset)
            guidAssets.Add(new GuidAsset<T>(item.Key));
        return guidAssets;
    }

}
