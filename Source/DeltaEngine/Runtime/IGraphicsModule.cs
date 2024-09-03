using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering.Headless;
using System.IO;

namespace Delta.Runtime;
public interface IGraphicsModule
{
    internal RenderBase RenderData { get; }
    internal void AddRenderBatcher(IRenderBatcher renderBatcher);
    internal void RemoveRenderBatcher(IRenderBatcher renderBatcher);
    public (int width, int height) Size { get; set; }
    internal void Execute();
    public void DrawGizmos(Render render, Transform transform);
    public void DrawMesh(Render render, Transform transform);
    public Stream RenderStream { get; }
}
