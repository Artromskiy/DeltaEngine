using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaEngine.Rendering;
internal class MeshCollection
{
    public Dictionary<Mesh, Dictionary<VertexAttribute, MeshVariant>> collection = new();

    public MeshVariant GetMeshVariant(Mesh mesh, VertexAttribute vertexMask)
    {
        _ = collection.TryGetValue(mesh, out var subCollection);
        _ = subCollection.TryGetValue(vertexMask, out var meshVariant);
        return meshVariant;
    }
}
