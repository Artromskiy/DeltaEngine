using Arch.Core;
using DeltaEngine.ECS;
using Silk.NET.Vulkan;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace DeltaEngine.Rendering;
internal class Renderer : BaseRenderer
{

    private static readonly Vector3 r = new(1.0f, 0.0f, 0.0f);
    private static readonly Vector3 g = new(0.0f, 1.0f, 0.0f);
    private static readonly Vector3 b = new(0.0f, 0.0f, 1.0f);

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
    private readonly GpuMappedSystem<TransformMapper, Transform, TRSData> _transformSystem;

    private readonly Fence _TRSCopyFence;
    private readonly Semaphore _TRSCopySemaphore;

    private CommandBuffer _TRSCopyCmdBuffer;

    public Renderer(World world, string appName) : base(appName)
    {
        _world = world;
        _transformSystem = new GpuMappedSystem<TransformMapper, Transform, TRSData>(_world, _rendererData);
        (_vertexBuffer, _vertexBufferMemory) = RenderHelper.CreateVertexBuffer(_rendererData, deltaLetterVertices);
        (_indexBuffer, _indexBufferMemory) = RenderHelper.CreateIndexBuffer(_rendererData, deltaLetterIndices);
        _TRSCopyCmdBuffer = RenderHelper.CreateCommandBuffer(_rendererData, _rendererData.deviceQueues.transferCmdPool);
        _TRSCopyFence = RenderHelper.CreateFence(_rendererData, false);
        _TRSCopySemaphore = RenderHelper.CreateSemaphore(_rendererData);
    }

    private readonly Stopwatch _updateDirty = new();
    private readonly Stopwatch _copyBufferSetup = new();
    private readonly Stopwatch _copyBuffer = new();
    private readonly Stopwatch _waitSync = new();

    public TimeSpan GetSyncMetric => _waitSync.Elapsed;
    public TimeSpan GetUpdateMetric => _updateDirty.Elapsed;
    public TimeSpan GetCopySetupMetric => _copyBufferSetup.Elapsed;
    public TimeSpan GetCopyMetric => _copyBuffer.Elapsed;

    public void ClearCounters()
    {
        _updateDirty.Reset();
        _copyBuffer.Reset();
        _copyBufferSetup.Reset();
        _waitSync.Reset();
        ClearAcquireMetric();
    }

    public override void Sync()
    {
        NextImage();
        PollEvents();
        _waitSync.Start();
        base.Sync();
        _waitSync.Stop();

        var fence = _TRSCopyFence;

        _copyBuffer.Start();
        _rendererData.vk.WaitForFences(_rendererData.deviceQueues.device, 1, in fence, true, ulong.MaxValue);
        _rendererData.vk.ResetCommandBuffer(_TRSCopyCmdBuffer, 0);
        _copyBuffer.Stop();

        _updateDirty.Start();
        _transformSystem.UpdateDirty();
        _updateDirty.Stop();

        _copyBufferSetup.Start();
        var frameTRS = GetTRSBuffer();
        _rendererData.CopyBuffer(_transformSystem, frameTRS, _TRSCopyFence, _TRSCopySemaphore, _TRSCopyCmdBuffer);
        _copyBufferSetup.Stop();

        AddSemaphore(_TRSCopySemaphore);
        SetBuffers(_vertexBuffer, _indexBuffer, (uint)deltaLetterIndices.Length);
        SetInstanceCount((uint)_transformSystem.Count);
    }

    public override void Run()
    {
        BeginFrameDraw();
    }

    private struct TRSData
    {
        public Vector4 position;
        public Quaternion rotation;
        public Vector4 scale;
    }


    private struct TransformMapper : IGpuMapper<Transform, TRSData>
    {
        [MethodImpl(Inl)]
        public readonly TRSData Map(ref Transform from) => new()
        {
            position = new(from.Position, 0),
            rotation = from.Rotation,
            scale = new(from.Scale, 0),
        };
    }
}
