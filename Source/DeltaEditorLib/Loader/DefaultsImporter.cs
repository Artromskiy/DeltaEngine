using Delta.Assets;
using Delta.Runtime;
using System.IO;

namespace DeltaEditorLib.Loader;

internal static class DefaultsImporter<T> where T: class, IAsset
{
    public static void Import(IRuntimeContext ctx, string directory)
    {
        foreach (var item in Directory.EnumerateFiles(directory))
        {
            try
            {
                var mesh = ctx.AssetImporter.LoadAsset<T>(item);
                var name = Path.ChangeExtension(Path.GetFileNameWithoutExtension(item), "mesh");
                ctx.AssetImporter.CreateAsset(mesh, name);
            }
            catch { }
        }
    }
}
