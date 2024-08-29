using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering.HeadlessRendering;
using Delta.Runtime;
using System;

namespace Delta.Rendering;
internal class DummyGraphics : IGraphicsModule
{
    RenderBase IGraphicsModule.RenderData => throw new NotImplementedException();
    void IGraphicsModule.AddRenderBatcher(IRenderBatcher renderBatcher) { }
    void IGraphicsModule.RemoveRenderBatcher(IRenderBatcher renderBatcher) { }
    void IGraphicsModule.Execute() { }

    public void DrawGizmos(Render render, Transform transform) { }
    public void DrawMesh(Render render, Transform transform) { }
}
