using Delta.Rendering;
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
        VC = AssetImporter.Instance.CreateRuntimeAsset(CreateShader());
        VCMat = AssetImporter.Instance.CreateRuntimeAsset(new MaterialData(VC));
    }

    private static ShaderData CreateShader()
    {
        var vert = File.ReadAllBytes(VCVert);
        var frag = File.ReadAllBytes(VCFrag);
        return new(vert, frag);
    }
}
