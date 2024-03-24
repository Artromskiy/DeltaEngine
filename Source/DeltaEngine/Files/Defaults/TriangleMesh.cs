using Delta.Rendering;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Delta.Files.Defaults;
internal class TriangleMesh
{
    private static readonly Vector4 r = new(1.0f, 0.0f, 0.0f, 1.0f);
    private static readonly Vector4 g = new(0.0f, 1.0f, 0.0f, 1.0f);
    private static readonly Vector4 b = new(0.0f, 0.0f, 1.0f, 1.0f);
    private static readonly Vector4[] colors = [b, g, r];
    private static readonly Vector2[] positions =
    [
        new(  0.00f,  -0.50f),
        new(  0.60f,   0.50f),
        new( -0.60f,   0.50f),
    ];
    private static readonly uint[] deltaLetterIndices =
    [
        0, 1, 2,
    ];

    public static readonly GuidAsset<MeshData> Mesh;
    public static MeshData MeshData => CreateMesh();

    static TriangleMesh()
    {
        Mesh = AssetImporter.Instance.CreateRuntimeAsset(CreateMesh());
    }

    private static MeshData CreateMesh()
    {
        byte[][] meshData = new byte[16][];
        meshData[VertexAttribute.Pos2.Location()] = MemoryMarshal.AsBytes(positions.AsSpan()).ToArray();
        meshData[VertexAttribute.Col.Location()] = MemoryMarshal.AsBytes(colors.AsSpan()).ToArray();
        return new(positions.Length, deltaLetterIndices, meshData);
    }
}
