using Delta.Runtime;
using System.IO;

namespace Delta.Assets.Defaults;

public static class DefaultsImporter<T> where T : class, IAsset
{
    public static void Import(string directory)
    {
        foreach (var item in Directory.EnumerateFiles(directory))
        {
            try
            {
                var mesh = IRuntimeContext.Current.AssetImporter.LoadAsset<T>(item);
                var name = Path.ChangeExtension(Path.GetFileNameWithoutExtension(item), "mesh");
                IRuntimeContext.Current.AssetImporter.CreateAsset(mesh, name);
            }
            catch { }
        }
    }
}
