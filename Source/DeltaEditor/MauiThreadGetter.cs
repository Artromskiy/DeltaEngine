using DeltaEditorLib.Loader;

namespace DeltaEditor;

internal class MauiThreadGetter : IUIThreadGetter
{
    public Func<Action, Task>? Thread
    {
        get
        {
            if (_thread == null && Application.Current != null)
                _thread = Application.Current.Dispatcher.DispatchAsync;
                //_thread = MainThread.InvokeOnMainThreadAsync;
            return _thread;
        }
    }

    private Func<Action, Task>? _thread;
}
