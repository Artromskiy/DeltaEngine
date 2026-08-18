using System;
using System.Threading.Tasks;
using Delta.Engine.EditorLib.Loader;

public interface IThreadGetter
{
    public Func<Action, Task>? Thread { get; }
}
