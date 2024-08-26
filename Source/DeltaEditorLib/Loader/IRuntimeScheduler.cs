using Delta.Runtime;
using System;

namespace DeltaEditorLib.Loader;

public interface IRuntimeScheduler
{
    public event Action<IRuntime> OnUIThreadLoop;
    public event Action<IRuntime> OnUIThread;
    public event Action<IRuntime> OnRuntimeThread;
}
