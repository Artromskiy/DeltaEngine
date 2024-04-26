using Delta.Runtime;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace DeltaEditorLib.Scripting;

internal class ExecutionModule : IExecutionModule
{
    private readonly IRuntime _runtime;
    private readonly IUIThreadGetter? _uiThreadGetter;

    private readonly List<Action<IRuntime>> _uiActionsLoop = [];
    private readonly List<Action<IRuntime>> _uiActions = [];
    private readonly ConcurrentQueue<Action<IRuntime>> _runtimeActions = [];

    public event Action<IRuntime> OnUIThreadLoop
    {
        add => _uiActionsLoop.Add(value);
        remove => _uiActionsLoop.Remove(value);
    }

    public event Action<IRuntime> OnUIThread
    {
        add => _uiActions.Add(value);
        remove => _ = 0;
    }

    public event Action<IRuntime> OnRuntimeThread
    {
        add => _runtimeActions.Enqueue(value);
        remove => _ = 0;
    }

    public ExecutionModule(IRuntime runtime, IUIThreadGetter? uiThreadGetter)
    {
        _runtime = runtime;
        _uiThreadGetter = uiThreadGetter;
        _runtime.RuntimeCall += Execute;
    }

    private void Execute()
    {
        RuntimeThreadCalls();
        UIThreadCall();
    }

    private void UIThreadCall()
    {
        try
        {
            _uiThreadGetter?.Thread?.Invoke(UIThreadCalls).Wait();
        }
        catch (Exception e)
        {
            Debug.Assert(true, e.Message);
        }
    }

    private void RuntimeThreadCalls()
    {
        while (_runtimeActions.TryDequeue(out var action))
            action.Invoke(_runtime);
    }

    private void UIThreadCalls()
    {
        foreach (var action in _uiActions)
            action.Invoke(_runtime);
        _uiActions.Clear();

        foreach (var action in _uiActionsLoop)
            action.Invoke(_runtime);
    }
}
