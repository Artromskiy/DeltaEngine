using Delta.Files;
using Delta.Files.Defaults;
using Delta.Scenes;
using System;

namespace Delta;

public sealed partial class Engine(string projectPath) : IDisposable
{
    private readonly AssetImporter _assetImporter = new(projectPath);
    private readonly JobScheduler.JobScheduler _jobScheduler = new("WorkerThread");
    private Scene _scene = new();
    private bool firstRun = true;

    public void CreateScene()
    {
        _scene?.Dispose();
        _scene = new Scene();
    }

    public void CreateTestScene()
    {
        _scene?.Dispose();
        _scene = TestScene.Scene;
    }

    public void Run()
    {
        _scene.Run();
        if (firstRun)
        {
            _scene._world.TrimExcess();
            GC.Collect();
            firstRun = false;
        }
    }

    public void CreateFile()
    {
        var mesh = DeltaMesh.Mesh;
        _assetImporter.CreateAsset("mesh", mesh.Asset);
    }

    public Scene GetCurrentScene() => _scene;
    public void Dispose() => _scene?.Dispose();
}