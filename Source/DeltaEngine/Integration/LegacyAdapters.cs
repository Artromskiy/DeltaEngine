namespace DVG.Engine.Runtime;

namespace DVG.Engine.Integration.Adapters;

public sealed class SceneWorldAdapter(Scene scene) : IEngineWorldService
{
    public void Initialize()
    {
    }

    public void Update(in EngineFrameContext context)
    {
        scene.Run(context.DeltaSeconds);
    }

    public void Shutdown()
    {
    }

    public void Dispose()
    {
        scene.Dispose();
    }
}

public sealed class LegacyRenderAdapter(IGraphicsModule graphicsModule) : IEngineRenderService
{
    public void Initialize()
    {
    }

    public void Render(in EngineFrameContext context)
    {
        graphicsModule.Execute();
    }

    public void Shutdown()
    {
    }

    public void Dispose()
    {
    }
}

public sealed class LegacyInputAdapter : IEngineInputService
{
    public void Initialize()
    {
    }

    public InputSnapshot PollInput(int frameNumber, float deltaSeconds)
    {
        return new InputSnapshot(frameNumber);
    }

    public void Shutdown()
    {
    }

    public void Dispose()
    {
    }
}

public sealed class LegacyUiAdapter : IEngineUiService
{
    public void Initialize()
    {
    }

    public void Update(in EngineFrameContext context)
    {
    }

    public void Shutdown()
    {
    }

    public void Dispose()
    {
    }
}
