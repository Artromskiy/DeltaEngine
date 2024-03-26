﻿using Delta.Rendering;
using Delta.Runtime;
using System.IO;

namespace Delta.Files.Defaults;
internal static class VCShader
{
    private const string VCVert = "shaders/vert.spv";
    private const string VCFrag = "shaders/frag.spv";

    public static readonly GuidAsset<ShaderData> VC;
    public static readonly GuidAsset<MaterialData> VCMat;

    static VCShader()
    {
        VC = IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(CreateShader());
        VCMat = IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(new MaterialData(VC));
    }

    private static ShaderData CreateShader()
    {
        var vert = File.ReadAllBytes(VCVert);
        var frag = File.ReadAllBytes(VCFrag);
        return new(vert, frag);
    }
}
