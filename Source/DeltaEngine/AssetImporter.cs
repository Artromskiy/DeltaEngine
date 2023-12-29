using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Delta.Files;

public class AssetImporter
{
    private readonly string ProjectFolder;
    private readonly string ResourcesFolder;

    private readonly Dictionary<Guid, string> _assetPaths = [];
    private readonly Dictionary<string, Guid> _pathToGuid = [];

    private const string MetaEnding = ".meta";
    private const string MetaSearch = "*.meta";

    private readonly Dictionary<Type, object> _assetCollections = [];
    private readonly RuntimeAssetCollection _runtimeAssetCollection = new();

    private static AssetImporter? _instance;
    public static AssetImporter Instance => _instance!;

    private readonly string _currentFolder;


    public AssetImporter() : this(Directory.GetCurrentDirectory()) { }
    public AssetImporter(string path)
    {
        _instance = this;
        ProjectFolder = path;
        ResourcesFolder = $"{path}{Path.DirectorySeparatorChar}Resources";
        _currentFolder = ResourcesFolder;
        Directory.CreateDirectory(ResourcesFolder);
        HashSet<string> allFiles = new(Directory.GetFiles(ProjectFolder, "*", SearchOption.AllDirectories));
        foreach (var item in allFiles)
        {
            if (item.EndsWith(MetaEnding))
            {
                using Stream fileStream = new FileStream(item, FileMode.Open, FileAccess.Read);
                var metaData = JsonSerializer.Deserialize<Meta>(fileStream);
                var assetPath = item[0..^MetaEnding.Length];
                _assetPaths.Add(metaData.guid, assetPath);
                _pathToGuid.Add(assetPath, metaData.guid);
            }
        }
        _assetCollections.Add(typeof(MeshData), new MeshCollection());
    }

    public GuidAsset<T> CreateAsset<T>(string name, T asset) where T : class, IAsset
    {
        string path = GetNextAvailableFilename(Path.Combine(_currentFolder, name));
        if (_pathToGuid.ContainsKey(path))
            return new GuidAsset<T>();

        var guid = Guid.NewGuid();
        var meta = new Meta(guid);

        using FileStream fileStream = File.Create(path);
        JsonSerializer.Serialize<T>(fileStream, asset);

        using FileStream metaStream = File.Create($"{path}{MetaEnding}");
        JsonSerializer.Serialize(metaStream, meta);

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
            alternateFilename = sb.Clear().Append(directory).Append(Path.DirectorySeparatorChar).Append(plainName).Append(fileNameIndex++).Append(extension).ToString();
        while (File.Exists(alternateFilename));

        return alternateFilename;
    }
}