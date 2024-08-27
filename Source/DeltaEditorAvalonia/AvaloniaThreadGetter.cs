using Avalonia.Threading;
using DeltaEditorLib.Loader;
using System;
using System.Threading.Tasks;

namespace DeltaEditorAvalonia;

internal class AvaloniaThreadGetter : IUIThreadGetter
{
    private Func<Action, Task>? _thread;
    public Func<Action, Task>? Thread
    {
        get
        {
            return _thread ??= static x => Dispatcher.UIThread.InvokeAsync(x, DispatcherPriority.Input).GetTask();
        }
    }
}
