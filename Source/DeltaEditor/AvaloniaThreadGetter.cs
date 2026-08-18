using Avalonia.Threading;
using DVG.Engine.EditorLib.Loader;
using System;
using System.Threading.Tasks;

namespace DVG.Engine.Editor;

internal class AvaloniaThreadGetter : IThreadGetter
{
    private Func<Action, Task>? _thread;
    public Func<Action, Task>? Thread => _thread ??= static x => Dispatcher.UIThread.InvokeAsync(x, DispatcherPriority.Input).GetTask();
}
