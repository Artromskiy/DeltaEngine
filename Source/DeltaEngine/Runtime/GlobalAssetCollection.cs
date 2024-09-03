using Delta.Files;
using System;
using System.Collections.Generic;

namespace Delta.Runtime;

internal class GlobalAssetCollection : IAssetCollection
{
    private readonly IProjectPath _projectPath;

    private readonly RuntimeAssetCollection _runtimeAssetCollection = new();
    private readonly Dictionary<Type, object> _typedAssetCollections = new()
    {
        {typeof(MeshData), new MeshCollection() },
        {typeof(ImageData), new ImageAssetCollection() }
    };

    public GlobalAssetCollection(IProjectPath projectPath)
    {
        _projectPath = projectPath;
    }

    public GuidAsset<T> CreateRuntimeAsset<T>(T asset) where T : class, IAsset
    {
        return GetAssetCollection<T>().CreateRuntimeAsset(asset);
    }

    public GuidAsset<T> CreateAsset<T>(string name, T asset) where T : class, IAsset
    {
        return GetAssetCollection<T>().CreateAsset(name, asset);
    }


    public T GetAsset<T>(GuidAsset<T> asset) where T : class, IAsset
    {
        return GetAssetCollection<T>().GetAsset(asset);
    }

    public List<GuidAsset<T>> GetAssets<T>() where T : class, IAsset
    {
        return GetAssetCollection<T>().GetAssets();
    }

    public string GetPath<T>(Guid guid) where T : class, IAsset
    {
        return GetAssetCollection<T>().GetPath(guid);
    }

    private IAssetCollection<T> GetAssetCollection<T>() where T : class, IAsset
    {
        var type = typeof(T);
        if (!_typedAssetCollections.TryGetValue(type, out var collection))
            _typedAssetCollections[type] = collection = new DefaultAssetCollection<T>();
        return (IAssetCollection<T>)collection;
    }
}