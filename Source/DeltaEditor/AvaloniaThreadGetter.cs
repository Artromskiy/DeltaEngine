using Avalonia.Threading;
using DeltaEditorLib.Loader;
using System;
using System.Threading.Tasks;

namespace DeltaEditor;

internal class AvaloniaThreadGetter : IThreadGetter
{
    private Func<Action, Task>? _thread;
    public Func<Action, Task>? Thread => _thread ??= static x => Dispatcher.UIThread.InvokeAsync(x, DispatcherPriority.Input).GetTask();
}
