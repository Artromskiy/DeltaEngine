using Silk.NET.Assimp;
using System;

namespace DeltaEngine.Rendering;


internal class MeshImporter : IDisposable
{
    private readonly Assimp _assimp = Assimp.GetApi();
    public void Dispose()
    {
        _assimp.Dispose();
    }

    /*

    public unsafe void Import(string path)
    {
        Model model = new Model();
        var p = PostProcessPreset.ConvertToLeftHanded;
        var scene = _assimp.ImportFile(path,
        //(uint)(PostProcessSteps.Triangulate)
        (uint)(PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals | PostProcessSteps.JoinIdenticalVertices)
            //(uint)(PostProcessSteps.JoinIdenticalVertices)
            );
        if (scene == null || scene->MFlags == Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
        {
            var error = _assimp.GetErrorStringS();
            throw new Exception(error);
        }

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
        int verticesCount = (int)mesh->MNumVertices;
        var m = new Mesh(new(mesh->MVertices, verticesCount), indices);
        if (mesh->MNormals != null)
            m.SetData(MeshData.Norm, new(mesh->MNormals, verticesCount * 3));
        if (mesh->MBitangents != null)
            m.SetData(MeshData.Bitan, new(mesh->MBitangents, verticesCount * 3));
        if (mesh->MTangents != null)
            m.SetData(MeshData.Tan, new(mesh->MTangents, verticesCount * 3));
        if (mesh->MColors[0] != null)
            m.SetData(MeshData.Color, new(mesh->MColors[0], verticesCount * 4));
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
