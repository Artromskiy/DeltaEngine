using System.Collections.Generic;

namespace DeltaEngine.Rendering;
internal class MeshData
{
    public int vertexCount;
    public readonly Dictionary<VertexAttribute, byte[]> verticesData = new();
    public readonly int[] indices = System.Array.Empty<int>();
}
