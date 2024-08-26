using Delta.Runtime;
using System;

namespace DeltaEditorLib.Loader;

public interface IRuntimeScheduler
{
    public event Action<IRuntimeContext> OnUIThreadLoop;
    public event Action<IRuntimeContext> OnUIThread;
    public event Action<IRuntimeContext> OnRuntimeThread;
}
