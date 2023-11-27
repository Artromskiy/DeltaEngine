using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeltaEngine.Files;

internal class MeshData
{
    [JsonInclude]
    public readonly int vertexCount;
    [JsonInclude]
    public readonly ImmutableArray<ImmutableArray<byte>> verticesData;
    [JsonInclude]
    public readonly ImmutableArray<uint> indices;

    public MeshData(uint[] indices, byte[][] vertices, int vertexCount)
    {
        this.vertexCount = vertexCount;
        this.indices = ImmutableArray.Create(indices);
        var builder = ImmutableArray.CreateBuilder<ImmutableArray<byte>>();
        for (int i = 0; i < vertices.GetLength(0); i++)
            builder.Add(ImmutableArray.Create(vertices[i]));
        verticesData = builder.ToImmutable();
    }
}
