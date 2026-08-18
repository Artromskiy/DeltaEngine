using Delta.Engine.ECS.Components;
using Delta.Engine.Rendering.Headless;
using Delta.Engine.Runtime;
using System;

using Delta.Engine.Rendering;
internal class DummyGraphics : IGraphicsModule
{
    RenderBase IGraphicsModule.RenderData => throw new NotImplementedException();
    public Memory<byte> RenderStream => Memory<byte>.Empty;
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
