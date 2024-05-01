using System.Collections.Generic;

namespace Delta.Rendering.RenderGraph;
internal abstract class RenderPass
{
    public abstract HashSet<RenderResource> ReadResources { get; init; }
    public abstract HashSet<RenderResource> WriteResources { get; init; }

    public abstract void Execute();
    public abstract void Setup();
}
