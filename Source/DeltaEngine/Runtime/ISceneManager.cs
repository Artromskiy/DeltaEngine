using System;

using DVG.Engine.Runtime;
public interface ISceneManager
{
    public event Action<Scene>? OnSceneChanged;
    public Scene CurrentScene { get; }
    public void LoadScene(string path);
    public void SaveScene(string name);
}
