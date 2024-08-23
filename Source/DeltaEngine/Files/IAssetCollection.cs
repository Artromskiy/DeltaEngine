using System.Collections.Generic;

namespace Delta.Files;
internal interface IAssetCollection<T> where T : class, IAsset
{
    public T LoadAsset(GuidAsset<T> guidAsset);
    public List<GuidAsset<T>> GetAssets();
}
