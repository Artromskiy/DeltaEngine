using System;

namespace Delta.Runtime;
internal class SceneManager : ISceneManager
{
    private Scene _scene;

    public Scene CurrentScene
    {
        get => _scene;
        private set => OnSceneChanged?.Invoke(_scene = value);
    }

    public event Action<Scene>? OnSceneChanged;

    public SceneManager()
    {
        _scene = new Scene();
    }

    public void LoadScene(string path)
    {
    }

    public void SaveScene(string name)
    {
        if (_scene != null)
            IRuntimeContext.Current.AssetImporter.CreateAsset(_scene, name);
    }

    public void CreateScene()
    {
    }

    public void CreateTestScene()
    {
        return;
    }
}