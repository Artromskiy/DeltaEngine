using System;

namespace Delta.Runtime;
public interface ISceneManager
{
    public event Action<Scene?>? OnSceneChanged;
    public Scene? CurrentScene { get; }
    public void LoadScene(string path);
    public void SaveScene(string name);
    public void CreateTestScene();
}
