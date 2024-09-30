using Delta.Runtime;
using DeltaEditorLib.Loader;
using System;
using System.Collections.Generic;
using System.Threading;

namespace DeltaEditorLib.Scripting;

internal sealed class RuntimeScheduler : IRuntimeScheduler, IDisposable
{
    private readonly IRuntime _runtime;
    private readonly IThreadGetter? _uiThreadGetter;
    private Thread? _runtimeThread;
    private bool _disposed = false;


    private readonly List<Action> _actionsLoop = [];


    public event Action OnLoop
    {
        add => _actionsLoop.Add(value);
        remove => _actionsLoop.Remove(value);
    }

    public RuntimeScheduler(IRuntime runtime, IThreadGetter? uiThreadGetter)
    {
        _runtime = runtime;
        _uiThreadGetter = uiThreadGetter;
    }

    public void Init()
    {
        _runtimeThread = new Thread(Loop);
        _runtimeThread.Name = "RuntimeThread." + _runtimeThread.ManagedThreadId;
        _runtimeThread.Start();
    }

    private void Loop()
    {
        while (!_disposed)
        {
            if (_uiThreadGetter != null && _uiThreadGetter.Thread != null)
                _uiThreadGetter.Thread(Execute).Wait();
            else
                Execute();
        }
    }

    private void Execute()
    {
        foreach (var action in _actionsLoop)
            action.Invoke();
        _runtime.Run();
    }

    public void Dispose()
    {
        _disposed = true;
    }
}