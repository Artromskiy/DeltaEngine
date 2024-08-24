using Arch.Core;
using Arch.Core.Extensions;
using Delta.Files.Defaults;
using Delta.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Delta.Runtime;
internal class SceneManager : ISceneManager
{
    private Scene? _scene;
    public Scene? CurrentScene
    {
        get => _scene;
        private set => OnSceneChanged?.Invoke(_scene = value);
    }
    public event Action<Scene?>? OnSceneChanged;

    private bool _firstRun = false;
    public void Execute()
    {
        if (_scene == null)
            return;

        _scene.Run();

        if (_firstRun)
        {
            _firstRun = false;
            _scene._world.TrimExcess();
            GC.Collect();
        }
    }

    public List<EntityReference> GetEntities()
    {
        if (_scene == null || _scene._world == null)
            return [];

        var archetypes = _scene._world.Archetypes;
        List<EntityReference> references = [];
        foreach (var item in archetypes)
            foreach (var chunk in item)
                references.AddRange(chunk.Entities[..chunk.Size].Select(e => e.Reference()));
        return references;
    }

    public void LoadScene(string path)
    {
        _scene?.Dispose();
        _scene = IRuntimeContext.Current.AssetImporter.GetAsset<Scene>(path);
        OnSceneChanged?.Invoke(_scene);
        _firstRun = true;
    }

    public void SaveScene(string name)
    {
        IRuntimeContext.Current.AssetImporter.CreateAsset<Scene>(name, _scene);
    }

    public void CreateScene()
    {
        _scene?.Dispose();
        _scene = new Scene();
        OnSceneChanged?.Invoke(_scene);
    }

    public void CreateTestScene()
    {
        _scene?.Dispose();
        _scene = TestScene.Scene;
        OnSceneChanged?.Invoke(_scene);
    }
}