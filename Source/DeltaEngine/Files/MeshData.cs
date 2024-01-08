using System;
using System.Text.Json.Serialization;

namespace Delta.Files;

public class MeshData : IAsset
{
    [JsonInclude]
    public readonly int vertexCount;
    [JsonInclude]
    private readonly byte[][] vertices;
    [JsonInclude]
    private readonly uint[] indices;

    [JsonIgnore]
    public ReadOnlySpan<uint> Indices => indices;
    public ReadOnlySpan<byte> GetAttributeArray(int index) => vertices[index];

    public MeshData(uint[] indices, byte[][] vertices, int vertexCount)
    {
        this.vertexCount = vertexCount;
        this.indices = (uint[])indices.Clone();
        this.vertices = new byte[vertices.GetLength(0)][];
        for (int i = 0; i < vertices.GetLength(0); i++)
            if (vertices[i] != null)
                this.vertices[i] = (byte[])vertices[i].Clone();
    }
}
