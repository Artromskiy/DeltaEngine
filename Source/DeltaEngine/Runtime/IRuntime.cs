using System;

namespace Delta.Runtime;
public interface IRuntime : IDisposable
{
    public IRuntimeContext Context { get; }
    public event Action? RuntimeCall;
}
