using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DeltaEngine.Rendering;

internal class MeshImporter : IDisposable
{
    //private static readonly Assimp _assimp = Assimp.GetApi();

    private readonly Dictionary<Guid, Dictionary<VertexAttribute, WeakReference<byte[]?>>> _meshMapVariants = new();
    private readonly Dictionary<Guid, WeakReference<MeshData?>> _meshDataMap = new();

    //private const PostProcessSteps importMode = PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.JoinIdenticalVertices;

    public void Dispose()
    {
        //_assimp.Dispose();
    }


    public unsafe MeshData GetMeshData(Guid guid)
    {
        if (!_meshDataMap.TryGetValue(guid, out var reference))
            _meshDataMap[guid] = reference = new(null);
        if (!reference.TryGetTarget(out var meshData))
            reference.SetTarget(meshData = LoadMesh(guid));
        return meshData;
        //
        //var p = PostProcessPreset.ConvertToLeftHanded;
        //var scene = _assimp.ImportFile(path, (uint)importMode);
        //_ = scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null;
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
            ref var attribArray = ref MemoryMarshal.GetArrayDataReference(meshData.verticesData[attrib]);
            int attribSize = attrib.GetAttributeSize();
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

    /*

    public unsafe void Import(string path)
    {
        Model model = new Model();

        ProcessNode(scene->MRootNode, scene, model);
        return model;
    }


    private unsafe void ProcessNode(Node* node, Scene* scene, Model model)
    {
        for (var i = 0; i < node->MNumMeshes; i++)
        {
            var mesh = scene->MMeshes[node->MMeshes[i]];
            model.Meshes.Add(ProcessMesh(mesh, scene));
        }

        for (var i = 0; i < node->MNumChildren; i++)
        {
            ProcessNode(node->MChildren[i], scene, model);
        }
    }

    private unsafe Mesh ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene)
    {
        uint indicesCount = 0;
        for (uint i = 0; i < mesh->MNumFaces; i++)
            indicesCount += mesh->MFaces[i].MNumIndices;
        Span<uint> indices = stackalloc uint[(int)indicesCount];
        int indexNum = 0;
        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            int count = (int)mesh->MFaces[i].MNumIndices;
            new Span<uint>(mesh->MFaces[i].MIndices, count).CopyTo(indices[indexNum..]);
            indexNum += count;
        }
        int vertexSize = (int)mesh->MNumVertices;
        var m = new Mesh(new(mesh->MVertices, vertexSize), indices);
        if (mesh->MNormals != null)
            m.SetData(MeshData.Norm, new(mesh->MNormals, vertexSize * 3));
        if (mesh->MBitangents != null)
            m.SetData(MeshData.Bitan, new(mesh->MBitangents, vertexSize * 3));
        if (mesh->MTangents != null)
            m.SetData(MeshData.Tan, new(mesh->MTangents, vertexSize * 3));
        if (mesh->MColors[0] != null)
            m.SetData(MeshData.Color, new(mesh->MColors[0], vertexSize * 4));
        m.Lock();
        
        return m;

    }


    private float[] BuildVertices(List<Vertex> vertexCollection)
    {
        var vertices = new List<float>();

        foreach (var vertex in vertexCollection)
        {
            vertices.Add(vertex.Position.X);
            vertices.Add(vertex.Position.Y);
            vertices.Add(vertex.Position.Z);
            vertices.Add(vertex.TexCoords.X);
            vertices.Add(vertex.TexCoords.Y);
        }

        return vertices.ToArray();
    }

    private uint[] BuildIndices(List<uint> indices)
    {
        return indices.ToArray();
    }
}
    */
}
