using Delta.Runtime;
using DeltaEditorLib.Loader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DeltaEditorLib.Scripting;

internal class RuntimeScheduler : IRuntimeScheduler
{
    private readonly IRuntime _runtime;
    private readonly IUIThreadGetter? _uiThreadGetter;

    private readonly List<Action<IRuntimeContext>> _uiActionsLoop = [];
    private readonly Queue<Action<IRuntimeContext>> _uiActions = [];
    private readonly ConcurrentQueue<Action<IRuntimeContext>> _runtimeActions = [];


    public event Action<IRuntimeContext> OnUIThreadLoop
    {
        add => _uiActionsLoop.Add(value);
        remove => _uiActionsLoop.Remove(value);
    }

    public event Action<IRuntimeContext> OnUIThread
    {
        add => _uiActions.Enqueue(value);
        remove => _ = 0;
    }

    public event Action<IRuntimeContext> OnRuntimeThread
    {
        add => _runtimeActions.Enqueue(value);
        remove => _ = 0;
    }

    public RuntimeScheduler(IRuntime runtime, IUIThreadGetter? uiThreadGetter)
    {
        _runtime = runtime;
        _uiThreadGetter = uiThreadGetter;
        _runtime.RuntimeCall += Execute;
    }

    private void Execute()
    {
        UIThreadCall();
        RuntimeThreadCalls();
    }

    private void UIThreadCall()
    {
        try
        {
            if (_uiActions.Count == 0 && _uiActionsLoop.Count == 0)
                return;

            _uiThreadGetter?.Thread?.Invoke(UIThreadCalls)?.Wait();
            // Somehow ui can stop respond
            // if game thread asks to update ui too freaquently
            // Maybe rendering not blocks other threads
            // and just endlessly waits for ui
            //Thread.Sleep(1);
        }
        catch (Exception e)
        {
            Debug.Assert(true, e.Message);
        }
    }

    private void RuntimeThreadCalls()
    {
        while (_runtimeActions.TryDequeue(out var action))
            action.Invoke(_runtime.Context);
    }

    private void UIThreadCalls()
    {
        while (_uiActions.TryDequeue(out var action))
            action.Invoke(_runtime.Context);

        foreach (var action in _uiActionsLoop)
            action.Invoke(_runtime.Context);
    }
}