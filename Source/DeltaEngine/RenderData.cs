using DeltaEngine.Files;
using DeltaEngine.Rendering;

namespace DeltaEngine;

internal class RenderData
{
    public Transform transform;
    public GuidAsset<MeshData> mesh;
    public GuidAsset<MaterialData> material;
    public bool isStatic;
}
