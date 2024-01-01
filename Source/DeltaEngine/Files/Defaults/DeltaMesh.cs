using Delta.Rendering;
using System;
using System.IO;
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
    private const string VCVert = "shaders/vert.spv";
    private const string VCFrag = "shaders/frag.spv";


    public static readonly GuidAsset<MeshData> Mesh;
    public static readonly GuidAsset<ShaderData> VC;
    public static readonly GuidAsset<MaterialData> VCMat;

    static DeltaMesh()
    {
        Mesh = AssetImporter.Instance.CreateRuntimeAsset(CreateMesh());
        VC = AssetImporter.Instance.CreateRuntimeAsset(CreateShader());
        VCMat = AssetImporter.Instance.CreateRuntimeAsset(new MaterialData(VC));
    }

    private static MeshData CreateMesh()
    {
        byte[][] meshData = new byte[16][];
        meshData[VertexAttribute.Pos2.Location()] = MemoryMarshal.AsBytes(positions.AsSpan()).ToArray();
        meshData[VertexAttribute.Col.Location()] = MemoryMarshal.AsBytes(colors.AsSpan()).ToArray();
        return new(deltaLetterIndices, meshData, positions.Length);
    }

    private static ShaderData CreateShader()
    {
        var vert = File.ReadAllBytes(VCVert);
        var frag = File.ReadAllBytes(VCFrag);
        return new(vert, frag);
    }
}
