namespace Delta.Files;
internal interface IAssetCollection<T> where T : class, IAsset
{
    public string GetPath(GuidAsset<T> guid);
    public string GetName(GuidAsset<T> guid);
    public T GetAsset(GuidAsset<T> guidAsset);

    public GuidAsset<T> CreateAsset(T asset, string name);
    public GuidAsset<T> CreateRuntimeAsset(T asset, string? name);

    public GuidAsset<T>[] GetRuntimeAssets();
    public int GetRuntimeAssetsCount();
    public GuidAsset<T>[] GetAssets();
    public int GetAssetsCount();
    public GuidAsset<T>[] GetAllAssets();
    public int GetAllAssetsCount();
}
