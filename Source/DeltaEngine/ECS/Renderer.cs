using Arch.Core;
using Delta.ECS;
using Delta.Files.Defaults;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;


namespace Delta.Rendering;
internal class Renderer : BaseRenderer
{
    private readonly Buffer _vertexBuffer;
    private readonly DeviceMemory _vertexBufferMemory;
    private readonly uint vertexCount;
    private readonly Buffer _indexBuffer;
    private readonly DeviceMemory _indexBufferMemory;
    private readonly uint indicesCount;

    private readonly Fence _TRSCopyFence;
    private readonly Semaphore _TRSCopySemaphore;

    private CommandBuffer _TRSCopyCmdBuffer;

    private readonly Batcher _batcher;

    public Renderer(World world, string appName) : base(appName)
    {
        (_vertexBuffer, _vertexBufferMemory) = RenderHelper.CreateVertexBuffer(_rendererData, DeltaMesh.Mesh.Asset, VertexAttribute.Pos2 | VertexAttribute.Col);
        (_indexBuffer, _indexBufferMemory) = RenderHelper.CreateIndexBuffer(_rendererData, DeltaMesh.Mesh.Asset);
        vertexCount = (uint)DeltaMesh.Mesh.Asset.vertexCount;
        indicesCount = (uint)DeltaMesh.Mesh.Asset.Indices.Length;

        _TRSCopyCmdBuffer = RenderHelper.CreateCommandBuffer(_rendererData, _rendererData.deviceQ.transferCmdPool);
        _TRSCopyFence = RenderHelper.CreateFence(_rendererData, true);
        _TRSCopySemaphore = RenderHelper.CreateSemaphore(_rendererData);

        _batcher = new Batcher(world, base._rendererData);
    }

    public override void PreSync()
    {
        _rendererData.vk.WaitForFences(_rendererData.deviceQ.device, 1, _TRSCopyFence, true, ulong.MaxValue);
        _rendererData.vk.ResetCommandBuffer(_TRSCopyCmdBuffer, 0);

        _batcher.Execute();
    }

    public sealed override void PostSync()
    {
        _rendererData.CopyBuffer(_batcher.Trs, GetTRSBuffer(), _TRSCopyFence, _TRSCopySemaphore, _TRSCopyCmdBuffer);

        AddSemaphore(_TRSCopySemaphore);
        SetBuffers(_vertexBuffer, _indexBuffer, indicesCount, vertexCount);
        SetInstanceCount(_batcher.TrsCount);
    }
}
