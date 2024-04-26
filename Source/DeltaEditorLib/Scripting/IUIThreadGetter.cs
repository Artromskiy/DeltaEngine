namespace DeltaEditorLib.Scripting;

public interface IUIThreadGetter
{
    public Func<Action, Task>? Thread { get; }
}
