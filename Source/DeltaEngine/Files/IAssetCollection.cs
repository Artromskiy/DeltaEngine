namespace DeltaEngine.Files;
internal interface IAssetCollection<T> where T : IAsset
{
    public T LoadAsset(GuidAsset<T> guidAsset);
}
