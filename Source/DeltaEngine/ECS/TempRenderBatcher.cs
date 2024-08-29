using Delta.ECS.Components;
using Delta.Rendering;
using Delta.Rendering.Collections;
using Delta.Rendering.HeadlessRendering;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Delta.ECS;
internal class TempRenderBatcher : IRenderBatcher
{
    public GpuArray<GpuCameraData> Camera { get; private set; }
    public GpuArray<Matrix4x4> Transforms { get; private set; }
    public GpuArray<uint> TransformIds { get; private set; }
    public ReadOnlySpan<(Render rend, uint count)> RendGroups => CollectionsMarshal.AsSpan(_rendersToCount);

    private readonly List<(Render rend, uint count)> _rendersToCount = [];
    private readonly List<(Render rend, Matrix4x4 matrix)> _tempRenders = [];

    public GpuCameraData CameraData { get; set; }

    public TempRenderBatcher(RenderBase renderBase)
    {
        Camera = new GpuArray<GpuCameraData>(renderBase.vk, renderBase.deviceQ, 1);
        TransformIds = new GpuArray<uint>(renderBase.vk, renderBase.deviceQ, 1);
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

            trs[(uint)i] = _tempRenders[i].matrix;
            ids[(uint)i] = (uint)i;
        }

        _tempRenders.Clear();

        Camera.Writer[0] = CameraData;
    }

}