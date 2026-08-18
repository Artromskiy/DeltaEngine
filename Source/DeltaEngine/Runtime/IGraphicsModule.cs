using DVG.Engine.ECS.Components;
using DVG.Engine.Rendering;
using DVG.Engine.Rendering.Headless;
using System;

using DVG.Engine.Runtime;
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
