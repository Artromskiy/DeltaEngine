using Delta.Runtime;

namespace DeltaEditorLib.Scripting;

public interface IExecutionModule
{
    public event Action<IRuntime> OnUIThreadLoop;
    public event Action<IRuntime> OnUIThread;
    public event Action<IRuntime> OnRuntimeThread;
}
