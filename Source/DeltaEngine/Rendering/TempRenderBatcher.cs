using Delta.ECS.Components;
using Delta.Rendering.Collections;
using Delta.Rendering.Headless;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Delta.Rendering;
internal class TempRenderBatcher : IRenderBatcher
{
    public GpuArray<GpuCameraData> Camera { get; private set; }
    public GpuArray<Matrix4x4> Transforms { get; private set; }
    public GpuArray<int> TransformIds { get; private set; }
    public ReadOnlySpan<(Render rend, int count)> RendGroups => CollectionsMarshal.AsSpan(_rendersToCount);

    private readonly List<(Render rend, int count)> _rendersToCount = [];
    private readonly List<(Render rend, Matrix4x4 matrix)> _tempRenders = [];

    public GpuCameraData CameraData { get; set; }

    public ReadOnlySpan<GpuByteArray> Buffers => throw new NotImplementedException();

    public ReadOnlySpan<DescriptorSetLayout> Layouts => throw new NotImplementedException();

    public ReadOnlySpan<int> Bindings => throw new NotImplementedException();

    public ReadOnlySpan<int> Sets => throw new NotImplementedException();

    public PipelineLayout PipelineLayout => throw new NotImplementedException();


    public TempRenderBatcher(RenderBase renderBase)
    {
        Camera = new GpuArray<GpuCameraData>(renderBase.vk, renderBase.deviceQ, 1);
        TransformIds = new GpuArray<int>(renderBase.vk, renderBase.deviceQ, 1);
        Transforms = new GpuArray<Matrix4x4>(renderBase.vk, renderBase.deviceQ, 1);
    }

    public void Dispose()
    {
        Camera.Dispose();
        TransformIds.Dispose();
        Transforms.Dispose();
    }

    public void Draw(Render render, Matrix4x4 matrix) => _tempRenders.Add((render, matrix));

    public void Execute()
    {
        _rendersToCount.Clear();
        if (_tempRenders.Count == 0)
            return;

        _tempRenders.Sort((x1, x2) => x1.rend.CompareTo(x2.rend));

        Render current = _tempRenders[0].rend;
        _rendersToCount.Add((current, 0));

        var trs = Transforms.Writer;
        var ids = TransformIds.Writer;
        for (int i = 0; i < _tempRenders.Count; i++)
        {
            if (current == _tempRenders[i].rend)
                _rendersToCount[^1] = (current, _rendersToCount[^1].count + 1);
            else
                _rendersToCount.Add((current = _tempRenders[i].rend, 1));

            trs[i] = _tempRenders[i].matrix;
            ids[i] = i;
        }

        _tempRenders.Clear();

        Camera.Writer[0] = CameraData;
    }

}