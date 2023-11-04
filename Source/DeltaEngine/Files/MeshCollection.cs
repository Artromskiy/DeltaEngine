using DeltaEngine.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DeltaEngine.Files;
internal class MeshCollection
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

    private static byte[] GetMeshVariant(MeshData meshData, VertexAttribute vertexMask)
    {
        int vertexSize = vertexMask.GetVertexSize();
        var sizeInBytes = vertexSize * meshData.vertexCount;
        byte[] result = new byte[sizeInBytes];
        ref var resultRef = ref MemoryMarshal.GetArrayDataReference(result);
        int innerOffset = 0;
        foreach (var attrib in vertexMask.Iterate())
        {
            ref var attribArray = ref MemoryMarshal.GetArrayDataReference(meshData.verticesData[attrib.location]);
            int attribSize = attrib.size;
            for (int i = 0; i < vertexSize; i++)
            {
                ref var source = ref Unsafe.Add(ref attribArray, (attribSize * i) + innerOffset);
                ref var destination = ref Unsafe.Add(ref resultRef, i * vertexSize);
                Unsafe.CopyBlockUnaligned(ref source, ref destination, (uint)attribSize);
            }
            innerOffset += attribSize;
        }
        return result;
    }
}
