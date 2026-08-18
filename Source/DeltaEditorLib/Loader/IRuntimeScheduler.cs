using System;

namespace DVG.Engine.EditorLib.Loader;

public interface IRuntimeScheduler
{
    public event Action OnLoop;
    public void Init();
}
