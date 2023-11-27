using System;

namespace DeltaEngine.Files;
internal interface IAssetCollection<T>
{
    public T LoadAsset(Guid guid);
}
