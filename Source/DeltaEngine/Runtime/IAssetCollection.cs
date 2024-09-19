using Delta.Assets;

namespace Delta.Runtime;

public interface IAssetCollection
{
    public T LoadAsset<T>(string path) where T : class, IAsset;
    public void SaveAsset<T>(T asset, string path) where T : class, IAsset;

    public string GetPath<T>(GuidAsset<T> guid) where T : class, IAsset;
    public string GetName<T>(GuidAsset<T> guid) where T : class, IAsset;

    public T GetAsset<T>(GuidAsset<T> asset) where T : class, IAsset;
    public GuidAsset<T> CreateAsset<T>(T asset, string name) where T : class, IAsset;
    public GuidAsset<T> CreateRuntimeAsset<T>(T asset, string? name = null) where T : class, IAsset;
    public GuidAsset<T>[] GetRuntimeAssets<T>() where T : class, IAsset;
    public GuidAsset<T>[] GetAssets<T>() where T : class, IAsset;
    public GuidAsset<T>[] GetAllAssets<T>() where T : class, IAsset;
}
