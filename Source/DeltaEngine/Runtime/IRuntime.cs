using System;

namespace Delta.Runtime;
public interface IRuntime : IDisposable
{
    public IAssetImporter AssetImporter { get; }
    public IProjectPath ProjectPath { get; }
    public bool Running { get; set; }
    public void CreateTestScene();
    public void CreateScene();
    public void SaveScene(string name);
}
