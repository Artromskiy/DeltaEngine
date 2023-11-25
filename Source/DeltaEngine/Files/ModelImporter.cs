using DeltaEngine.Rendering;
using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace DeltaEngine.Files;

internal class ModelImporter : IAssetImporter, IDisposable
{
    private static readonly Assimp _assimp = Assimp.GetApi();
    private const PostProcessSteps importMode = PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.JoinIdenticalVertices;
    public ImmutableHashSet<string> FileFormats { get; } = ImmutableHashSet.Create("fbx");
    public void Dispose() => _assimp.Dispose();

    public unsafe void Import(string path)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        Scene* scene = _assimp.ImportFile(path, (uint)importMode);
        List<(MeshData meshData, string name)> meshDatas = new();
        ProcessScene(scene->MRootNode, scene, meshDatas);
        foreach (var (meshData, name) in meshDatas)
            AssetImporter.Instance.CreateAsset($"{fileName}.{name}.mesh", meshData);
    }

    private unsafe void ProcessScene(Node* node, Scene* scene, List<(MeshData meshData, string name)> meshDatas)
    {
        for (var i = 0; i < node->MNumMeshes; i++)
            meshDatas.Add(ProcessMesh(scene->MMeshes[node->MMeshes[i]], scene));
        for (var i = 0; i < node->MNumChildren; i++)
            ProcessScene(node->MChildren[i], scene, meshDatas);
    }

    private unsafe (MeshData data, string name) ProcessMesh(Silk.NET.Assimp.Mesh* mesh, Scene* scene)
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
        byte[][] meshData = new byte[16][];
        if (mesh->MVertices != null)
            meshData[VertexAttribute.Pos3.Location()] = new Span<byte>(mesh->MNormals, vertexCount * VertexAttribute.Pos3.Size()).ToArray();
        if (mesh->MNormals != null)
            meshData[VertexAttribute.Norm.Location()] = new Span<byte>(mesh->MNormals, vertexCount * VertexAttribute.Norm.Size()).ToArray();
        if (mesh->MBitangents != null)
            meshData[VertexAttribute.Bitan.Location()] = new Span<byte>(mesh->MBitangents, vertexCount * VertexAttribute.Bitan.Size()).ToArray();
        if (mesh->MTangents != null)
            meshData[VertexAttribute.Tan.Location()] = new Span<byte>(mesh->MTangents, vertexCount * VertexAttribute.Tan.Size()).ToArray();
        if (mesh->MColors[0] != null)
            meshData[VertexAttribute.Col.Location()] = new Span<byte>(mesh->MColors[0], vertexCount * VertexAttribute.Col.Size()).ToArray();

        return (new MeshData(indices.ToArray(), meshData, vertexCount), mesh->MName);
    }
}
