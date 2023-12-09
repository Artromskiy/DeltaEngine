using DeltaEngine.Files;
using DeltaEngine.Rendering;

namespace DeltaEngine;

internal struct Render
{
    public GuidAsset<ShaderData> shader;
    public GuidAsset<MaterialData> material;
    public GuidAsset<MeshData> mesh;
}
