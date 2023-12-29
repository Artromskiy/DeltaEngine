namespace Delta.Files;
internal interface IAssetCollection<T> where T : class, IAsset
{
    public T LoadAsset(GuidAsset<T> guidAsset);
}
