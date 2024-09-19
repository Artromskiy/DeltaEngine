using Delta.Rendering;
using Delta.Runtime;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Delta.Assets.Defaults;
public class TriangleMesh
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

    internal static GuidAsset<MeshData> Mesh => IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(MeshData);
    public static void Init() => IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(MeshData, "Triangle");
    internal static MeshData MeshData
    {
        get
        {
            byte[][] meshData = new byte[16][];
            var pos3 = Array.ConvertAll(positions, x => new Vector3(x.X, x.Y, 0));
            meshData[VertexAttribute.Pos2.GetAttributeLocation()] = MemoryMarshal.AsBytes(positions.AsSpan()).ToArray();
            meshData[VertexAttribute.Col.GetAttributeLocation()] = MemoryMarshal.AsBytes(colors.AsSpan()).ToArray();
            meshData[VertexAttribute.Pos3.GetAttributeLocation()] = MemoryMarshal.AsBytes(new ReadOnlySpan<Vector3>(pos3)).ToArray();
            return new(positions.Length, deltaLetterIndices, meshData);
        }
    }
}
