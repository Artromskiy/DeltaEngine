using Delta.Runtime;
using System.IO;

namespace Delta.Assets.Defaults;
public static class VCShader
{
    private const string VCVert = "shaders/vert.spv";
    private const string VCFrag = "shaders/frag.spv";

    internal static GuidAsset<MaterialData> VCMat
    {
        get
        {
            var VC = IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(CreateShader());
            return IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(new MaterialData(VC));
        }
    }

    public static void Init()
    {
        var VC = IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(CreateShader(), "VertexColor2DShader");
        IRuntimeContext.Current.AssetImporter.CreateRuntimeAsset(new MaterialData(VC), "VertexColor2DMaterial");
    }

    private static ShaderData CreateShader()
    {
        var vert = File.ReadAllBytes(VCVert);
        var frag = File.ReadAllBytes(VCFrag);
        return new(vert, frag);
    }
}
