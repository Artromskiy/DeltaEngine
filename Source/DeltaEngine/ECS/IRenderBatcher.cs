using Delta.ECS.Components;
using Delta.Rendering;
using Delta.Rendering.Collections;
using System;
using System.Numerics;

namespace Delta.ECS;
internal interface IRenderBatcher : IDisposable
{
    /// <summary>
    /// Contains matrices with world position of each <see cref="Render"/>
    /// </summary>
    public GpuArray<Matrix4x4> Transforms { get; }
    /// <summary>
    /// Contains indices to elements of <see cref="Transforms"/> ordered by <see cref="Render"/>
    /// </summary>
    public GpuArray<uint> TransformIds { get; } // send to compute to sort trs on device
    /// <summary>
    /// Contains information about Camera
    /// </summary>
    public GpuArray<GpuCameraData> Camera { get; }

    ReadOnlySpan<(Render rend, uint count)> RendGroups { get; }
    void Execute();
}
