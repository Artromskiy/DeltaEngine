using System;
using System.Threading.Tasks;
namespace DeltaEditorLib.Loader;

public interface IThreadGetter
{
    public Func<Action, Task>? Thread { get; }
}
