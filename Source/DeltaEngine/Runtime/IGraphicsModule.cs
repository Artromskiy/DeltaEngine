using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering.Internal;

namespace Delta.Runtime;
public interface IGraphicsModule
{
    internal RenderBase RenderData { get; }
    internal void AddRenderBatcher(IRenderBatcher renderBatcher);
    internal void RemoveRenderBatcher(IRenderBatcher renderBatcher);
    internal void Execute();
    public void DrawGizmos(Render render, Transform transform);
    public void DrawMesh(Render render, Transform transform);
}
