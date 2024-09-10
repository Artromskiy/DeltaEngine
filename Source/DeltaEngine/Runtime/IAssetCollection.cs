using Delta.Files;

namespace Delta.Runtime;

public interface IAssetCollection
{
    public GuidAsset<T> CreateAsset<T>(string name, T asset) where T : class, IAsset;
    public GuidAsset<T> CreateRuntimeAsset<T>(T asset) where T : class, IAsset;
    public T GetAsset<T>(GuidAsset<T> asset) where T : class, IAsset;
}
