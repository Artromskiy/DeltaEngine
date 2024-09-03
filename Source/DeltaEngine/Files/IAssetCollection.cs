using System;
using System.Collections.Generic;

namespace Delta.Files;
internal interface IAssetCollection<T> where T : class, IAsset
{
    public T GetAsset(GuidAsset<T> guidAsset);
    public T GetAsset(string path);
    public GuidAsset<T> CreateAsset(string name, T asset);
    public GuidAsset<T> CreateRuntimeAsset(T asset);
    public string GetPath(Guid guid);
    public List<GuidAsset<T>> GetAssets();
}
