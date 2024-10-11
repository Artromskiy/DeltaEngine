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
    private readonly int[] _sets;
    private readonly PipelineLayout _pipelineLayout;

    public abstract ReadOnlySpan<GpuByteArray> Buffers { get; }
    public abstract ReadOnlySpan<(Render rend, int count)> RendGroups { get; }
    public ReadOnlySpan<DescriptorSetLayout> Layouts => _layouts;
    public ReadOnlySpan<int> Bindings => _bindings;
    public ReadOnlySpan<int> Sets => _sets;
    public PipelineLayout PipelineLayout => _pipelineLayout;

    public abstract void Dispose();
    public abstract void Execute();

    public abstract JaggedReadOnlySpan<int> DescriptorSetsBindings { get; }

    public RenderBatcher()
    {
        var setsBindings = DescriptorSetsBindings;
        var setsCount = setsBindings.Length;
        _layouts = new DescriptorSetLayout[setsCount];
        int bindingsCount = 0;
        for (int i = 0; i < setsCount; bindingsCount += setsBindings[i].Length, i++)
            _layouts[i] = RenderHelper.CreateDescriptorSetLayout(setsBindings[i]);

        _bindings = new int[bindingsCount];
        _sets = new int[bindingsCount];

        bindingsCount = 0;
        for (int i = 0; i < setsCount; i++)
        {
            int bindingsILength = setsBindings[i].Length;
            for (int j = 0; j < bindingsILength; bindingsCount++, j++)
            {
                _sets[bindingsCount] = i;
                _bindings[bindingsCount] = setsBindings[i][j];
            }
        }

        _pipelineLayout = RenderHelper.CreatePipelineLayout(Layouts);
    }
}
