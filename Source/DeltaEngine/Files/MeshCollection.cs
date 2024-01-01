using Delta.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Delta.Files;
internal class MeshCollection : IAssetCollection<MeshData>
{
    private readonly Dictionary<Guid, Dictionary<VertexAttribute, WeakReference<byte[]?>>> _meshMapVariants = new();
    private readonly Dictionary<Guid, WeakReference<MeshData?>> _meshDataMap = new();

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

    private static MeshData LoadMesh(Guid guid)
    {
        var path = AssetImporter.Instance.GetPath(guid);
        using Stream s = new FileStream(path, FileMode.Open, FileAccess.Read);
        return JsonSerializer.Deserialize<MeshData>(s);
    }

    public unsafe byte[] GetMeshVariant(VertexAttribute vertexMask, Guid guid)
    {
        if (!_meshMapVariants.TryGetValue(guid, out var meshVariants))
            _meshMapVariants[guid] = meshVariants = new();
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
        ref var resultRef = ref MemoryMarshal.GetArrayDataReference(result);
        int offset = 0;
        foreach (var attrib in vertexMask.Iterate())
        {
            ref var attribArray = ref MemoryMarshal.GetArrayDataReference(meshData.vertices[attrib.location]);
            int attribSize = attrib.size;
            for (int i = 0; i < meshData.vertexCount; i++)
            {
                ref var source = ref Unsafe.Add(ref attribArray, attribSize * i);
                ref var destination = ref Unsafe.Add(ref resultRef, i * vertexSize + offset);
                Unsafe.CopyBlockUnaligned(ref destination, ref source, (uint)attribSize);
            }
            offset += attribSize;
        }
        return result;
    }
}