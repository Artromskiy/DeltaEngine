using Delta.Scenes;

namespace Delta.Runtime;
public interface ISceneManager
{
    public Scene? CurrentScene { get; }
    public void Execute(float deltaTime);
    public void LoadScene(string path);
    public void SaveScene(string name);
    public void CreateTestScene();
    public bool Running { get; set; }
}
