using DeltaEditorLib.Loader;

namespace DeltaEditor;

internal class MauiThreadGetter : IUIThreadGetter
{
    private Func<Action, Task>? _thread;
    public Func<Action, Task>? Thread
    {
        get
        {
            if (_thread == null && Application.Current != null)
                _thread = Application.Current.Dispatcher.DispatchAsync;
            return _thread;
        }
    }
}
