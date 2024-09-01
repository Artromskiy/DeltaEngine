using Delta.Rendering;
using Delta.Runtime;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Delta.Files.Defaults;
internal static class DeltaMesh
{
    private static readonly Vector4 r = new(1.0f, 0.0f, 0.0f, 1.0f);
    private static readonly Vector4 g = new(0.0f, 1.0f, 0.0f, 1.0f);
    private static readonly Vector4 b = new(0.0f, 0.0f, 1.0f, 1.0f);
    private static readonly Vector4[] colors = [b, g, r, r, b, g];
    private static readonly Vector2[] positions =
    [
        new(  0.00f,  -0.50f),
        new(  0.60f,   0.50f),
        new( -0.60f,   0.50f),
        new(  0.00f,  -0.25f),
        new(  0.35f,   0.35f),
        new( -0.35f,   0.35f)
    ];
    private static readonly uint[] deltaLetterIndices =
    [
        0, 1, 3,
        1, 2, 4,
        2, 0, 5,
        3, 1, 4,
        4, 2, 5,
        5, 0, 3
    ];

    public static GuidAsset<MeshData> Mesh => IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(MeshData);

    public static MeshData MeshData
    {
        get
        {
            byte[][] meshData = new byte[16][];
            meshData[VertexAttribute.Pos2.GetAttributeLocation()] = MemoryMarshal.AsBytes(positions.AsSpan()).ToArray();
            meshData[VertexAttribute.Col.GetAttributeLocation()] = MemoryMarshal.AsBytes(colors.AsSpan()).ToArray();
            return new(positions.Length, deltaLetterIndices, meshData);
        }
    }
}
