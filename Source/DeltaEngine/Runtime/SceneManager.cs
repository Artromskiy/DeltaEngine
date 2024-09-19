using Delta.Assets.Defaults;
using System;

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

    public void LoadScene(string path)
    {
        _scene?.Dispose();
        OnSceneChanged?.Invoke(_scene);
    }

    public void SaveScene(string name)
    {
        if (_scene != null)
            IRuntimeContext.Current.AssetImporter.CreateAsset<Scene>(_scene, name);
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