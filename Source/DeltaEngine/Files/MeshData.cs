using System;
using System.Text.Json.Serialization;

namespace DeltaEngine.Files;

internal class MeshData
{
    [JsonInclude]
    public readonly int vertexCount;
    [JsonInclude]
    public readonly byte[][] verticesData;
    [JsonInclude]
    public readonly uint[] indices = Array.Empty<uint>();

    public MeshData(uint[] indices, byte[][] vertices, int vertexCount)
    {
        this.vertexCount = vertexCount;
        this.indices = (uint[])indices.Clone();
        verticesData = (byte[][])vertices.Clone();
    }

    public UInt128 CheckSum()
    {
        UInt128 checkSum = 0;
        for (int i = 0; i < verticesData.GetLength(0); i++)
            checkSum += verticesData[i].AsSpan().CheckSum();
        checkSum += indices.AsSpan().CheckSum();
        checkSum += (uint)vertexCount;
        return checkSum;
    }
}
