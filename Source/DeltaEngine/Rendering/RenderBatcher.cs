using Delta.ECS.Components;
using Delta.Rendering.Collections;
using Delta.Utilities;
using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering;
internal abstract class RenderBatcher : IRenderBatcher
{
    private readonly DescriptorSetLayout[] _layouts;
    private readonly int[] _bindings;
    private readonly PipelineLayout _pipelineLayout;

    public abstract ReadOnlySpan<GpuByteArray> Buffers { get; set; }
    public abstract ReadOnlySpan<(Render rend, int count)> RendGroups { get; }
    public ReadOnlySpan<DescriptorSetLayout> Layouts => _layouts;
    public ReadOnlySpan<int> Bindings => _bindings;
    public PipelineLayout PipelineLayout => _pipelineLayout;

    public abstract void Dispose();
    public abstract void Execute();

    public abstract JaggedSpan<int> DescriptorSetBindings { get; set; }

    public RenderBatcher()
    {
        var bindings = DescriptorSetBindings;
        _layouts = new DescriptorSetLayout[bindings.Length];
        int bindingsCount = 0;
        for (int i = 0; i < bindings.Length; bindingsCount += bindings[i].Length, i++)
            _layouts[i] = RenderHelper.CreateDescriptorSetLayout(bindings[i]);
        _bindings = new int[bindingsCount];

        bindingsCount = 0;
        for (int i = 0; i < bindings.Length; i++)
            for (int j = 0; j < bindings[i].Length; j++, bindingsCount++)
                _bindings[bindingsCount] = bindings[i][j];

        _pipelineLayout = RenderHelper.CreatePipelineLayout(Layouts);
    }
}
