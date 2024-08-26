using System;
using System.Threading.Tasks;
namespace DeltaEditorLib.Loader;

public interface IUIThreadGetter
{
    public Func<Action, Task>? Thread { get; }
}
