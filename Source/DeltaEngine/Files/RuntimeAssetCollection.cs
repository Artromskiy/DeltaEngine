﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Delta.Files;

internal class RuntimeAssetCollection
{
    private readonly Dictionary<Guid, string> _assetPaths = [];
    private readonly Dictionary<string, Guid> _pathToGuid = [];
    private readonly string _currentFolder;
    private const string MetaEnding = ".meta";

    public RuntimeAssetCollection()
    {
        _currentFolder = Directory.CreateTempSubdirectory().FullName;
    }

    public GuidAsset<T> CreateAsset<T>(T asset) where T : class, IAsset
    {
        var guid = Guid.NewGuid();

        string path = AssetImporter.GetNextAvailableFilename(Path.Combine(_currentFolder, guid.ToString()));
        if (_pathToGuid.ContainsKey(path))
            return new GuidAsset<T>();

        var meta = new Meta(guid);

        using FileStream fileStream = File.Create(path);
        JsonSerializer.Serialize<T>(fileStream, asset);

        using FileStream metaStream = File.Create($"{path}{MetaEnding}");
        JsonSerializer.Serialize(metaStream, meta);

        _assetPaths.Add(guid, path);
        _pathToGuid.Add(path, guid);
        return new GuidAsset<T>(guid);
    }

    public string GetPath(Guid guid)
    {
        if (!_assetPaths.TryGetValue(guid, out string? result))
            result = string.Empty;
        return result;
    }
}
