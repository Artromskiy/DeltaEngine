using DeltaEngine.Rendering;

namespace DeltaEngine.Files;
internal readonly struct MeshVariant
{
    public readonly Mesh mesh;
    public readonly VertexAttribute vertexMask;

    public MeshVariant(Mesh mesh, VertexAttribute vertexMask)
    {
        this.mesh = mesh;
        this.vertexMask = vertexMask;
    }

    public MeshVariant(Mesh mesh, ShaderData shader)
    {
        this.mesh = mesh;
        vertexMask = shader.vertexMask;
    }
}
