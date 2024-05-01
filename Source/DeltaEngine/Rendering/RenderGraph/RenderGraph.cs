using System.Collections.Generic;

namespace Delta.Rendering.RenderGraph;
internal class RenderGraph
{
    private readonly List<RenderPass> _renderPasses = [];
    public void AddPass(RenderPass pass)
    {
        _renderPasses.Add(pass);
    }
    public void RemovePass(RenderPass pass)
    {
        _renderPasses.Remove(pass);
    }
}
