using Delta.Files;
using System;
using System.Collections.Generic;

namespace Delta.Runtime;

internal class GlobalAssetCollection : IAssetCollection
{
    private readonly Dictionary<Type, object> _typedAssetCollections = new()
    {
        {typeof(MeshData), new MeshCollection() },
    };

    public string GetPath<T>(GuidAsset<T> asset) where T : class, IAsset =>
        GetAssetCollection<T>().GetPath(asset);
    public string GetName<T>(GuidAsset<T> asset) where T : class, IAsset =>
        GetAssetCollection<T>().GetName(asset);
    public T GetAsset<T>(GuidAsset<T> asset) where T : class, IAsset =>
        GetAssetCollection<T>().GetAsset(asset);
    public GuidAsset<T> CreateAsset<T>(T asset, string name) where T : class, IAsset =>
        GetAssetCollection<T>().CreateAsset(asset, name);
    public GuidAsset<T> CreateRuntimeAsset<T>(T asset, string? name = null) where T : class, IAsset =>
        GetAssetCollection<T>().CreateRuntimeAsset(asset, name);
    public GuidAsset<T>[] GetRuntimeAssets<T>() where T : class, IAsset =>
        GetAssetCollection<T>().GetRuntimeAssets();
    public int GetRuntimeAssetsCount<T>() where T : class, IAsset =>
        GetAssetCollection<T>().GetRuntimeAssetsCount();
    public GuidAsset<T>[] GetAssets<T>() where T : class, IAsset =>
        GetAssetCollection<T>().GetAssets();
    public int GetAssetsCount<T>() where T : class, IAsset =>
        GetAssetCollection<T>().GetAssetsCount();
    public GuidAsset<T>[] GetAllAssets<T>() where T : class, IAsset =>
        GetAssetCollection<T>().GetAllAssets();
    public int GetAllAssetsCount<T>() where T : class, IAsset =>
        GetAssetCollection<T>().GetAllAssetsCount();

    private IAssetCollection<T> GetAssetCollection<T>() where T : class, IAsset
    {
        var type = typeof(T);
        if (!_typedAssetCollections.TryGetValue(type, out var collection))
            _typedAssetCollections[type] = collection = new DefaultAssetCollection<T>();
        return (IAssetCollection<T>)collection;
    }
}