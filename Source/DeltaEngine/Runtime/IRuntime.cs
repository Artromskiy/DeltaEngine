using Arch.Core;
using System;
using System.Collections.Generic;

namespace Delta.Runtime;
public interface IRuntime : IDisposable
{
    public IRuntimeContext Context { get; }
    public bool Running { get; set; }
    public void RunOnce();
    public void CreateTestScene();
    public void CreateScene();
    public void SaveScene(string name);
    public List<EntityReference> GetEntities();

    public PauseHandle Pause { get; }
}
