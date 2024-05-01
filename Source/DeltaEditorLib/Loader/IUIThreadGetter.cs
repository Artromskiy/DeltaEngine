namespace DeltaEditorLib.Loader;

public interface IUIThreadGetter
{
    public Func<Action, Task>? Thread { get; }
}
