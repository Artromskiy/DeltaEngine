using Delta.ECS.Components;
using Delta.Rendering.Headless;
using Delta.Runtime;
using System;

namespace Delta.Rendering;
internal class DummyGraphics : IGraphicsModule
{
    RenderBase IGraphicsModule.RenderData => throw new NotImplementedException();
    public Memory<byte> RenderStream => throw new NotImplementedException();
    public (int width, int height) Size
    {
        get => default;
        set => _ = value;
    }

    void IGraphicsModule.AddRenderBatcher(IRenderBatcher renderBatcher) { }
    void IGraphicsModule.RemoveRenderBatcher(IRenderBatcher renderBatcher) { }
    void IGraphicsModule.Execute() { }

    public void DrawGizmos(Render render, Transform transform) { }
    public void DrawMesh(Render render, Transform transform) { }

}
