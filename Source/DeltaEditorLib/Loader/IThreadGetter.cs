using System;
using System.Threading.Tasks;
using DVG.Engine.EditorLib.Loader;

public interface IThreadGetter
{
    public Func<Action, Task>? Thread { get; }
}
