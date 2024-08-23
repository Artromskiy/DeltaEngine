using Delta.Files;
using System;

namespace Delta.Runtime;

public interface IAssetCollection
{
    public GuidAsset<T> CreateAsset<T>(string name, T asset) where T : class, IAsset;
    public GuidAsset<T> CreateRuntimeAsset<T>(T asset) where T : class, IAsset;
    public T GetAsset<T>(GuidAsset<T> asset) where T : class, IAsset;
    public T GetAsset<T>(string path) where T : class, IAsset;
    public string GetPath(Guid guid);
}
