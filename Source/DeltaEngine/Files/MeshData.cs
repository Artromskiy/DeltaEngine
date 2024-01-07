using System.Text.Json.Serialization;

namespace Delta.Files;

public class MeshData : IAsset
{
    // TODO implement access as readonly span
    [JsonInclude]
    public readonly int vertexCount;
    [JsonInclude]
    public readonly byte[][] vertices;
    [JsonInclude]
    public readonly uint[] indices;

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
