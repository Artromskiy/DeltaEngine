using System;

namespace Delta.Runtime;
public interface IRuntime : IDisposable
{
    public IRuntimeContext Context { get; }

    public bool Running { get; set; }
    public void CreateTestScene();
    public void CreateScene();
    public void SaveScene(string name);
}
