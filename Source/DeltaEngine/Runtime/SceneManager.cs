using Arch.Core;
using Delta.Scenes;
using Schedulers;
using System;

namespace Delta.Runtime;
internal class SceneManager : ISceneManager
{
    private Scene? _scene;
    private bool _firstRun = false;
    private JobScheduler.Config _jobConfig = new()
    {
        ThreadPrefixName = "Arch.Multithreading",
        ThreadCount = 0,
        MaxExpectedConcurrentJobs = 64,
        StrictAllocationMode = false,
    };

    public void Execute()
    {
        if (_scene == null)
            return;

        World.SharedJobScheduler ??= new JobScheduler(_jobConfig);
        _scene.Run();

        if (_firstRun)
        {
            _firstRun = false;
            _scene._world.TrimExcess();
            GC.Collect();
        }
    }

    public void LoadScene(string path)
    {
        _scene?.Dispose();
        _scene = IRuntimeContext.Current.AssetImporter.GetAsset<Scene>(path);
        _firstRun = false;
    }

    public void SaveScene(string name)
    {
        IRuntimeContext.Current.AssetImporter.CreateAsset<Scene>(name, _scene);
    }
}
