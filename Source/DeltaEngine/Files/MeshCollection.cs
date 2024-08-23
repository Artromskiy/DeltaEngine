using Delta.Rendering;
using Delta.Runtime;
using System;
using System.Collections.Generic;

namespace Delta.Files;
internal class MeshCollection : IAssetCollection<MeshData>
{
    private readonly Dictionary<Guid, Dictionary<VertexAttribute, WeakReference<byte[]?>>> _meshMapVariants = [];
    private readonly Dictionary<Guid, WeakReference<MeshData?>> _meshDataMap = [];

    public unsafe MeshData GetMeshData(Guid guid)
    {
        if (!_meshDataMap.TryGetValue(guid, out var reference))
            _meshDataMap[guid] = reference = new(null);
        if (!reference.TryGetTarget(out var meshData))
            reference.SetTarget(meshData = LoadMesh(guid));
        return meshData;
    }

    public MeshData LoadAsset(GuidAsset<MeshData> guidAsset)
    {
        if (!_meshDataMap.TryGetValue(guidAsset.guid, out var reference))
            _meshDataMap[guidAsset.guid] = reference = new(null);
        if (!reference.TryGetTarget(out var meshData))
            reference.SetTarget(meshData = LoadMesh(guidAsset.guid));
        return meshData;
    }

    public List<GuidAsset<MeshData>> GetAssets()
    {
        List<GuidAsset<MeshData>> assets = [];
        foreach (var item in _meshDataMap)
            assets.Add(new GuidAsset<MeshData>(item.Key));
        return assets;
    }

    private static MeshData LoadMesh(Guid guid)
    {
        return Serialization.Deserialize<MeshData>(IRuntimeContext.Current.AssetImporter.GetPath(guid));
    }

    public unsafe byte[] GetMeshVariant(VertexAttribute vertexMask, Guid guid)
    {
        if (!_meshMapVariants.TryGetValue(guid, out var meshVariants))
            _meshMapVariants[guid] = meshVariants = [];
        if (!meshVariants.TryGetValue(vertexMask, out var reference))
            meshVariants[vertexMask] = reference = new(null);
        if (!reference.TryGetTarget(out var result))
            reference.SetTarget(result = GetMeshVariant(GetMeshData(guid), vertexMask));
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