using Delta.Engine.ECS.Components;
using Delta.Engine.Rendering;
using Delta.Engine.Rendering.Headless;
using System;

using Delta.Engine.Runtime;
public interface IGraphicsModule
{
    internal RenderBase RenderData { get; }
    internal void AddRenderBatcher(IRenderBatcher renderBatcher);
    internal void RemoveRenderBatcher(IRenderBatcher renderBatcher);
    public (int width, int height) Size { get; set; }
    internal void Execute();
    public void DrawGizmos(Render render, Transform transform);
    public void DrawMesh(Render render, Transform transform);
    public Memory<byte> RenderStream { get; }
}
