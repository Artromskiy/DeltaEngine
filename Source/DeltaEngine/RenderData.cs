using DeltaEngine.Files;
using DeltaEngine.Rendering;

namespace DeltaEngine;

public struct RenderData
{
    public Transform transform;
    public GuidAsset<MeshData> mesh;
    public GuidAsset<MaterialData> material;
    public bool isStatic;
}
