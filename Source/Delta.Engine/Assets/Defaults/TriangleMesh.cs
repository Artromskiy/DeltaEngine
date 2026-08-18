using Delta.Engine.Rendering;
using Delta.Engine.Runtime;
using Delta.Maths;
using System;
using System.Runtime.InteropServices;

using Delta.Engine.Assets.Defaults;
public class TriangleMesh
{
    private static readonly float4 r = new(1.0f, 0.0f, 0.0f, 1.0f);
    private static readonly float4 g = new(0.0f, 1.0f, 0.0f, 1.0f);
    private static readonly float4 b = new(0.0f, 0.0f, 1.0f, 1.0f);
    private static readonly float4[] colors = [b, g, r];
    private static readonly float2[] positions =
    [
        new(  0.00f,  0.50f),
        new(  0.60f, -0.50f),
        new( -0.60f, -0.50f),
    ];
    private static readonly uint[] deltaLetterIndices =
    [
        0, 1, 2,
    ];

    internal static GuidAsset<MeshData> Mesh => IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(MeshData);
    public static void Init() => IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(MeshData, "Triangle.mesh");
    internal static MeshData MeshData
    {
        get
        {
            byte[][] meshData = new byte[16][];
            var pos3 = Array.ConvertAll(positions, x => new float3(x.x, x.y, 0));
            meshData[VertexAttribute.Pos2.GetAttributeLocation()] = MemoryMarshal.AsBytes(positions.AsSpan()).ToArray();
            meshData[VertexAttribute.Col.GetAttributeLocation()] = MemoryMarshal.AsBytes(colors.AsSpan()).ToArray();
            meshData[VertexAttribute.Pos3.GetAttributeLocation()] = MemoryMarshal.AsBytes(new ReadOnlySpan<float3>(pos3)).ToArray();
            return new(positions.Length, deltaLetterIndices, meshData);
        }
    }
}
