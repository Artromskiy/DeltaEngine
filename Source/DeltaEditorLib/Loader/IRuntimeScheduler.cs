using Delta.Runtime;
using System;

namespace DeltaEditorLib.Loader;

public interface IRuntimeScheduler
{
    public event Action OnLoop;
    public void Init();
}
