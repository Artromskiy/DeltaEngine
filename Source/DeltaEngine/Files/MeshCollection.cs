using Delta.Rendering;
using Delta.Runtime;
using System;
using System.Collections.Generic;

namespace Delta.Files;
internal class MeshCollection : DefaultAssetCollection<MeshData>
{
    private readonly Dictionary<Guid, Dictionary<VertexAttribute, WeakReference<byte[]?>>> _meshMapVariants = [];
    private readonly Dictionary<Guid, WeakReference<MeshData?>> _meshDataMap = [];

    public unsafe byte[] GetMeshVariant(VertexAttribute vertexMask, Guid guid)
    {
        if (!_meshMapVariants.TryGetValue(guid, out var meshVariants))
            _meshMapVariants[guid] = meshVariants = [];
        if (!meshVariants.TryGetValue(vertexMask, out var reference))
            meshVariants[vertexMask] = reference = new(null);
        if (!reference.TryGetTarget(out var result))
            reference.SetTarget(result = GetMeshVariant(GetAsset(new GuidAsset<MeshData>(guid)), vertexMask));
        return result;
    }

    public static byte[] GetMeshVariant(MeshData meshData, VertexAttribute vertexMask)
    {
        int vertexSize = vertexMask.GetVertexSize();
        var meshSize = vertexSize * meshData.vertexCount;
        byte[] result = new byte[meshSize];
        int offset = 0;
        foreach (var attrib in vertexMask.Iterate())
        {
            var attribArray = meshData.GetAttributeArray(attrib.location);
            int attribSize = attrib.size;
            for (int i = 0; i < meshData.vertexCount; i++)
            {
                var source = attribArray.Slice(attribSize * i, attribSize);
                var destination = new Span<byte>(result, (i * vertexSize) + offset, attribSize);
                source.CopyTo(destination);
            }
            offset += attribSize;
        }
        return result;
    }
}