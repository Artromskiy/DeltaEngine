using Delta.ECS.Components;
using Delta.Rendering;
using System;
using System.Numerics;

namespace Delta.ECS;
internal interface IBatcher
{
    public void Execute();
    public GpuArray<uint> TrsIds { get; }
    public GpuArray<Matrix4x4> Trs { get; }
    public ReadOnlySpan<(Render render, uint count)> SortedRenderGroups { get; }
}
