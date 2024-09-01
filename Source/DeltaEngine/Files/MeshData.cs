using Delta.Rendering;
using System;
using System.Text.Json.Serialization;

namespace Delta.Files;

public class MeshData : IAsset
{
    public readonly int vertexCount;
    private readonly byte[][] vertices;
    private readonly uint[] indices;

    public uint GetIndicesCount() => (uint)indices.Length;
    public ReadOnlySpan<uint> GetIndices() => indices;
    public ReadOnlySpan<byte> GetAttributeArray(int index) => vertices[index];

    [JsonConstructor]
    public MeshData(int vertexCount, uint[] indices, byte[][] vertices)
    {
        this.vertexCount = vertexCount;
        this.indices = (uint[])indices.Clone();
        this.vertices = new byte[vertices.GetLength(0)][];
        for (int i = 0; i < vertices.GetLength(0); i++)
            if (vertices[i] != null)
                this.vertices[i] = (byte[])vertices[i].Clone();
    }

    public MeshData(int vertexCount, uint[] indices)
    {
        this.vertexCount = vertexCount;
        this.indices = indices;
        vertices = new byte[16][];
    }

    public unsafe void SetData(VertexAttribute attribute, void* dataPointer)
    {
        if (dataPointer != null)
            vertices[attribute.GetAttributeLocation()] = new Span<byte>(dataPointer, vertexCount * attribute.GetAttributeSize()).ToArray();
    }
}
