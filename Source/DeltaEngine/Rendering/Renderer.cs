﻿using Arch.Core;
using DeltaEngine.ECS;
using Silk.NET.Vulkan;
using System;
using System.Diagnostics;
using System.Numerics;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using static DeltaEngine.Rendering.ComponentMappers;


namespace DeltaEngine.Rendering;
internal class Renderer : BaseRenderer
{
    private static readonly Vector3 r = new(1.0f, 0.0f, 0.0f);
    private static readonly Vector3 g = new(0.0f, 1.0f, 0.0f);
    private static readonly Vector3 b = new(0.0f, 0.0f, 1.0f);

    private readonly Vertex[] deltaLetterVerticesUnindexed = [];

    private readonly Vertex[] deltaLetterVertices =
    {
        new (new(0.0f, -0.5f),   b),
        new (new(0.6f, 0.5f),    g),
        new (new(-0.6f, 0.5f),   r),
        new (new(0.0f, -0.25f),   r),
        new (new(0.35f, 0.35f),    b),
        new (new(-0.35f, 0.35f),   g),
    };
    private readonly uint[] deltaLetterIndices =
    {
        0,1,3,
        1,2,4,
        2,0,5,
        3,1,4,
        4,2,5,
        5,0,3
    };

    private readonly Buffer _vertexBuffer;
    private readonly DeviceMemory _vertexBufferMemory;
    private readonly Buffer _indexBuffer;
    private readonly DeviceMemory _indexBufferMemory;

    private readonly World _world;
    private readonly GpuMappedSystem<TransformMapper, Transform, TrsData> _TrsDatas;
    private readonly GpuMappedSystem<RenderMapper, Render, RendData> _RendDatas;

    private readonly Fence _TRSCopyFence;
    private readonly Semaphore _TRSCopySemaphore;

    private CommandBuffer _TRSCopyCmdBuffer;

    public Renderer(World world, string appName) : base(appName)
    {
        _world = world;
        _TrsDatas = new GpuMappedSystem<TransformMapper, Transform, TrsData>(_world, _rendererData);
        _RendDatas = new GpuMappedSystem<RenderMapper, Render, RendData>(_world, _rendererData);


        deltaLetterVerticesUnindexed = new Vertex[deltaLetterIndices.Length];
        for (int i = 0; i < deltaLetterIndices.Length; i++)
            deltaLetterVerticesUnindexed[i] = deltaLetterVertices[deltaLetterIndices[i]];

        (_vertexBuffer, _vertexBufferMemory) = RenderHelper.CreateVertexBuffer(_rendererData, deltaLetterVertices);
        (_indexBuffer, _indexBufferMemory) = RenderHelper.CreateIndexBuffer(_rendererData, deltaLetterIndices);

        _TRSCopyCmdBuffer = RenderHelper.CreateCommandBuffer(_rendererData, _rendererData.deviceQueues.transferCmdPool);
        _TRSCopyFence = RenderHelper.CreateFence(_rendererData, true);
        _TRSCopySemaphore = RenderHelper.CreateSemaphore(_rendererData);
    }

    private readonly Stopwatch _updateDirty = new();
    private readonly Stopwatch _copyBufferSetup = new();
    private readonly Stopwatch _copyBuffer = new();

    public TimeSpan GetUpdateMetric => _updateDirty.Elapsed;
    public TimeSpan GetCopySetupMetric => _copyBufferSetup.Elapsed;
    public TimeSpan GetCopyMetric => _copyBuffer.Elapsed;

    public override void ClearCounters()
    {
        base.ClearCounters();
        _updateDirty.Reset();
        _copyBuffer.Reset();
        _copyBufferSetup.Reset();
    }

    public override void PreSync()
    {
        _copyBuffer.Start();
        _rendererData.vk.WaitForFences(_rendererData.deviceQueues.device, 1, _TRSCopyFence, true, ulong.MaxValue);
        _rendererData.vk.ResetCommandBuffer(_TRSCopyCmdBuffer, 0);
        _copyBuffer.Stop();

        _updateDirty.Start();
        _TrsDatas.UpdateDirty();
        _updateDirty.Stop();
    }

    public sealed override void PostSync()
    {
        _copyBufferSetup.Start();
        _rendererData.CopyBuffer(_TrsDatas, GetTRSBuffer(), _TRSCopyFence, _TRSCopySemaphore, _TRSCopyCmdBuffer);
        _copyBufferSetup.Stop();

        AddSemaphore(_TRSCopySemaphore);
        SetBuffers(_vertexBuffer, _indexBuffer, (uint)deltaLetterIndices.Length, (uint)deltaLetterVerticesUnindexed.Length);
        SetInstanceCount((uint)_TrsDatas.Count);
    }
}
