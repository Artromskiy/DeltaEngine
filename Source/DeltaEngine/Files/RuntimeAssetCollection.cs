using Delta.Runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace Delta.Files;

internal class RuntimeAssetCollection
{
    private readonly Dictionary<Guid, string> _assetPaths = [];
    private readonly Dictionary<string, Guid> _pathToGuid = [];

    private readonly string _currentFolder;

    public RuntimeAssetCollection()
    {
        _currentFolder = Directory.CreateTempSubdirectory().FullName;
    }

    public GuidAsset<T> CreateAsset<T>(T asset) where T : class, IAsset
    {
        var guid = Guid.NewGuid();

        string path = FileHelper.CreateIndexedFile(_currentFolder, guid);
        if (_pathToGuid.ContainsKey(path))
            return new GuidAsset<T>();

        Serialization.Serialize(path, asset);

        _assetPaths.Add(guid, path);
        _pathToGuid.Add(path, guid);
        return new GuidAsset<T>(guid);
    }

    public GuidAsset<T> CreateAsset<T>(T asset, string extension) where T : class, IAsset
    {
        var guid = Guid.NewGuid();

        string path = FileHelper.CreateIndexedFile(_currentFolder, guid, extension);
        if (_pathToGuid.ContainsKey(path))
            return new GuidAsset<T>();

        Serialization.Serialize(path, asset);

        _assetPaths.Add(guid, path);
        _pathToGuid.Add(path, guid);
        return new GuidAsset<T>(guid);
    }

    public string GetPath(Guid guid) => _assetPaths.TryGetValue(guid, out string? result) ? result : string.Empty;
}