using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeltaEngine;
internal class AssetImporter
{
    private readonly string ProjectFolder;
    private readonly Dictionary<Guid, string> _assetPaths = new();
    private const string Meta = ".meta";
    private const string MetaSearch = "*.meta";

    public static AssetImporter Instance;

    static AssetImporter()
    {
        Instance = new(Directory.GetCurrentDirectory());
    }

    public AssetImporter(string path)
    {
        ProjectFolder = path;
        HashSet<string> noValidMeta = new();
        HashSet<string> noValidSource = new();
        HashSet<string> allFiles = new(Directory.GetFiles(ProjectFolder, "*", SearchOption.AllDirectories));
        foreach (var file in allFiles)
        {
            bool isMeta = file.EndsWith(Meta);
            bool valid = isMeta ? allFiles.Contains(file[..Meta.Length]) : allFiles.Contains(file + Meta);
            if (!valid)
                (isMeta ? noValidSource : noValidMeta).Add(file);
        }
        foreach (var item in allFiles)
        {
            if(item.EndsWith(Meta))
            {
                using Stream s = new FileStream(item, FileMode.Open, FileAccess.Read);
                var metaData = JsonSerializer.Deserialize<Meta>(s);
                _assetPaths.Add(metaData.guid, item);
            }
        }
    }

    public string GetPath(Guid guid)=> _assetPaths[guid];
}
