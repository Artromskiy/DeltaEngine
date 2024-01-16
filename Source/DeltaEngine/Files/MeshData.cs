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
}
