using DeltaEngine.Files;
using DeltaEngine.Rendering;

namespace DeltaEngine.ECS;
public struct MeshRenderer
{
    public GuidAsset<MeshData> mesh;
    public GuidAsset<MaterialData> material;
}
