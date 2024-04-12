namespace Delta.Runtime;


public readonly ref struct PauseHandle
{
    private readonly bool state;
    private readonly IRuntime _runtime;
    public PauseHandle(IRuntime runtime, bool value)
    {
        _runtime = runtime;
        state = _runtime.Running;
        _runtime.Running = !value;
    }

    public void Dispose()
    {
        _runtime.Running = state;
    }
}
