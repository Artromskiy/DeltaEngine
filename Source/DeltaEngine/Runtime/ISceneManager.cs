using Arch.Core;
using Delta.Scenes;
using System.Collections.Generic;

namespace Delta.Runtime;
public interface ISceneManager
{
    public Scene? CurrentScene { get; }
    public void Execute(float deltaTime);
    public List<EntityReference> GetEntities();
    public void LoadScene(string path);
    public void SaveScene(string name);
    public void CreateTestScene();
    public bool Running { get; set; }
}
