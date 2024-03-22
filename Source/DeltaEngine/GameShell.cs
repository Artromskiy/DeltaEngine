using Delta.Files;
using Delta.Scenes;

namespace Delta;
internal class GameShell
{
    private readonly SceneCollection _sceneCollection;

    private Scene _currentScene;

    public GameShell(string path)
    {
        _sceneCollection = new(path);
        var scenes = _sceneCollection.GetScenes();
        if (scenes.Length == 0)
            _currentScene = TestScene.Scene;
        else
            _currentScene = _sceneCollection.GetScenes()[0].GetAsset();
    }
}