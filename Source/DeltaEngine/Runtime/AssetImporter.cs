using Delta.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Delta.Runtime;

internal class AssetImporter : IAssetImporter
{
    private readonly IProjectPath _projectPath;

    private readonly Dictionary<Guid, string> _assetPaths = [];
    private readonly Dictionary<string, Guid> _pathToGuid = [];

    private const string MetaEnding = ".meta";
    private const string MetaSearch = "*.meta";

    private readonly RuntimeAssetCollection _runtimeAssetCollection = new();
    private readonly Dictionary<Type, object> _assetCollections = new()
    {
        {typeof(MeshData), new MeshCollection() },
    };

    public AssetImporter(IProjectPath projectPath)
    {
        _projectPath = projectPath;
    }

    public void InitFiles()
    {
        foreach (var item in Directory.EnumerateFiles(_projectPath.ResourcesDirectory, MetaSearch, SearchOption.AllDirectories))
        {
            using Stream fileStream = new FileStream(item, FileMode.Open, FileAccess.Read);

            var metaData = JsonSerializer.Deserialize<Meta>(fileStream);
            if (_assetPaths.ContainsKey(metaData.guid))
                continue;

            var assetPath = item[0..^MetaEnding.Length];
            if (_pathToGuid.ContainsKey(assetPath))
                continue;

            _assetPaths.Add(metaData.guid, assetPath);
            _pathToGuid.Add(assetPath, metaData.guid);
        }
    }

    public GuidAsset<T> CreateAsset<T>(string name, T asset) where T : class, IAsset
    {
        string path = GetNextAvailableFilename(Path.Combine(_projectPath.ResourcesDirectory, name));
        if (_pathToGuid.ContainsKey(path))
            return new GuidAsset<T>();

        var guid = Guid.NewGuid();
        var meta = new Meta(guid);

        Serialization.Serialize(path, asset);

        Serialization.Serialize($"{path}{MetaEnding}", meta);

        _assetPaths.Add(guid, path);
        _pathToGuid.Add(path, guid);
        return new GuidAsset<T>(guid);
    }

    public GuidAsset<T> CreateRuntimeAsset<T>(T asset) where T : class, IAsset
    {
        return _runtimeAssetCollection.CreateAsset(asset);
    }

    public T GetAsset<T>(GuidAsset<T> asset) where T : class, IAsset
    {
        var type = typeof(T);
        if (!_assetCollections.TryGetValue(type, out var collection))
            _assetCollections[type] = collection = new DefaultAssetCollection<T>();
        return ((IAssetCollection<T>)collection).LoadAsset(asset);
    }

    public T GetAsset<T>(string path) where T : class, IAsset
    {
        return GetAsset(new GuidAsset<T>(_pathToGuid[path]));
    }

    public string GetPath(Guid guid)
    {
        if (!_assetPaths.TryGetValue(guid, out string? result))
            return _runtimeAssetCollection.GetPath(guid);
        return result;
    }

    public static string GetNextAvailableFilename(string filename)
    {
        if (!File.Exists(filename))
            return filename;

        string alternateFilename;
        int fileNameIndex = 1;
        var filenameSpan = filename.AsSpan();
        var directory = Path.GetDirectoryName(filenameSpan);
        var plainName = Path.GetFileNameWithoutExtension(filenameSpan);
        var extension = Path.GetExtension(filenameSpan);

        StringBuilder sb = new();
        do
            sb.Clear().
            Append(directory).
            Append(Path.DirectorySeparatorChar).
            Append(plainName).
            Append(fileNameIndex++).
            Append(extension);
        while (File.Exists(alternateFilename = sb.ToString()));

        return alternateFilename;
    }
}