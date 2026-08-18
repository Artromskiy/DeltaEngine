using Delta.Engine.ECS.Components;
using Delta.Engine.Rendering.Collections;
using Silk.NET.Vulkan;
using System;

using Delta.Engine.Rendering;
internal interface IRenderBatcher : IDisposable
{
    public ReadOnlySpan<GpuByteArray> Buffers { get; }
    public ReadOnlySpan<DescriptorSetLayout> Layouts { get; }
    public ReadOnlySpan<int> Bindings { get; }
    public ReadOnlySpan<int> Sets { get; }
    public ReadOnlySpan<(Render rend, int count)> RendGroups { get; }
    public PipelineLayout PipelineLayout { get; }
    void Execute();
}
