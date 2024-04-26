using Arch.Core;
using System;
using System.Collections.Generic;

namespace Delta.Runtime;
public interface IRuntime : IDisposable
{
    public IRuntimeContext Context { get; }
    public event Action? RuntimeCall;
    public void CreateTestScene();
    public void CreateScene();
    public void SaveScene(string name);
    public List<EntityReference> GetEntities();
}
