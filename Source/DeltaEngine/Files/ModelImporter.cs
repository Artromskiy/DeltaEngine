using Delta.Rendering;
using Delta.Runtime;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Delta.Files;

public class ModelImporter : IDisposable
{
    private static readonly Assimp _assimp = Assimp.GetApi();
    private const PostProcessSteps importMode = PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.JoinIdenticalVertices;
    public ImmutableHashSet<string> FileFormats { get; } = ["fbx"];
    public void Dispose() => _assimp.Dispose();

    public unsafe void Import(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        Scene* scene = _assimp.ImportFile(path, (uint)importMode);
        List<(MeshData meshData, string name)> meshDatas = [];
        ProcessScene(scene->MRootNode, scene, meshDatas);
        foreach (var (meshData, name) in meshDatas)
            IRuntimeContext.Current.AssetImporter.CreateAsset(meshData, $"{fileName}.{name}.mesh");
    }

    public static unsafe List<(MeshData meshData, string name)> ImportAndGet(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        Scene* scene = _assimp.ImportFile(path, (uint)importMode);
        List<(MeshData meshData, string name)> meshDatas = [];
        ProcessScene(scene->MRootNode, scene, meshDatas);
        return meshDatas;
    }

    private static unsafe void ProcessScene(Node* node, Scene* scene, List<(MeshData meshData, string name)> meshDatas)
    {
        for (var i = 0; i < scene->MNumMeshes; i++)
            meshDatas.Add(ProcessMesh(scene->MMeshes[i]));
    }

    private static unsafe (MeshData data, string name) ProcessMesh(Mesh* mesh)
    {
        uint indicesCount = 0;
        for (uint i = 0; i < mesh->MNumFaces; i++)
            indicesCount += mesh->MFaces[i].MNumIndices;
        Span<uint> indices = stackalloc uint[(int)mesh->MNumFaces * 3];
        int indexNum = 0;
        for (uint i = 0; i < mesh->MNumFaces; i++)
        {
            int count = (int)mesh->MFaces[i].MNumIndices;
            if (count != 3)
                continue;
            new Span<uint>(mesh->MFaces[i].MIndices, count).CopyTo(indices[indexNum..]);
            indexNum += count;
        }
        int vertexCount = (int)mesh->MNumVertices;
        var meshData = new MeshData(vertexCount, indices.ToArray());
        meshData.SetData(VertexAttribute.Pos3, mesh->MVertices);
        meshData.SetData(VertexAttribute.Norm, mesh->MNormals);
        meshData.SetData(VertexAttribute.Bitan, mesh->MBitangents);
        meshData.SetData(VertexAttribute.Tan, mesh->MTangents);
        meshData.SetData(VertexAttribute.Col, mesh->MColors[0]);
        return (meshData, mesh->MName);
    }
}