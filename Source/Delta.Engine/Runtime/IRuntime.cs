using System;

using Delta.Engine.Runtime;
public interface IRuntime : IDisposable
{
    public IRuntimeContext Context { get; }
    public void Run();
}
