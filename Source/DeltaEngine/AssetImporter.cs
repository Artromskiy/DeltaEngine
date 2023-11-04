using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DeltaEngine.Files;
internal class AssetImporter
{
    private readonly string ProjectFolder;
    private readonly string ResourcesFolder;
    private readonly Dictionary<Guid, string> _assetPaths = new();
    private readonly Dictionary<string, Guid> _pathToGuid = new();
    //private readonly Dictionary<Guid, MetaEnding> _metas = new();
    private const string MetaEnding = ".meta";
    private const string MetaSearch = "*.meta";

    public static AssetImporter Instance { get; private set; }

    private string _currentFolder;

    static AssetImporter()
    {
        Instance = new(Directory.GetCurrentDirectory());
    }

    public AssetImporter(string path)
    {
        ProjectFolder = path;
        ResourcesFolder = $"{path}/Resources";
        _currentFolder = ResourcesFolder;
        Directory.CreateDirectory(ResourcesFolder);


        HashSet<string> noValidMeta = new();
        HashSet<string> noValidSource = new();
        HashSet<string> allFiles = new(Directory.GetFiles(ProjectFolder, "*", SearchOption.AllDirectories));
        //foreach (var file in allFiles)
        //{
        //    bool isMeta = file.EndsWith(MetaEnding);
        //    bool valid = isMeta ? allFiles.Contains(file[..MetaEnding.Length]) : allFiles.Contains(file + MetaEnding);
        //    if (!valid)
        //        (isMeta ? noValidSource : noValidMeta).Add(file);
        //}
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
    }

    public Guid CreateAsset<T>(string name, T asset)
    {
        string path = Path.Combine(_currentFolder, name);
        if (_pathToGuid.ContainsKey(path))
            return Guid.Empty;

        var guid = Guid.NewGuid();
        var meta = new Meta(guid);

        using FileStream fileStream = File.Create(path);
        JsonSerializer.Serialize<T>(fileStream, asset);

        using FileStream metaStream = File.Create($"{path}{MetaEnding}");
        JsonSerializer.Serialize(metaStream, meta);
        
        _assetPaths.Add(guid, path);
        _pathToGuid.Add(path, guid);
        return guid;
    }

    public string GetPath(Guid guid) => _assetPaths[guid];
}
