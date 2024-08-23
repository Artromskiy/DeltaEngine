using Delta.ECS;
using Delta.Rendering.Internal;
using Delta.Runtime;
using System;

namespace Delta.Rendering;
internal class DummyGraphics : IGraphicsModule
{
    RenderBase IGraphicsModule.RenderData => throw new NotImplementedException();
    void IGraphicsModule.AddRenderBatcher(IRenderBatcher renderBatcher) { }
    void IGraphicsModule.RemoveRenderBatcher(IRenderBatcher renderBatcher) { }
    void IGraphicsModule.Execute() { }
}
