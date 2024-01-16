using Arch.Core;
using Delta.ECS;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;


namespace Delta.Rendering;
internal class Renderer : BaseRenderer
{
    private readonly Fence _TRSCopyFence;
    private readonly Semaphore _TRSCopySemaphore;

    private CommandBuffer _TRSCopyCmdBuffer;

    private readonly Batcher _batcher;

    public Renderer(World world, string appName) : base(appName)
    {
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
        _rendererData.vk.ResetFences(_rendererData.deviceQ.device, 1, _TRSCopyFence);
        _rendererData.BeginCmdBuffer(_TRSCopyCmdBuffer);
        _rendererData.CopyCmd(_batcher.Trs, GetTRSBuffer(), _TRSCopyCmdBuffer);
        _rendererData.CopyCmd(_batcher.TrsIds, GetIdsBuffer(), _TRSCopyCmdBuffer);
        _rendererData.EndCmdBuffer(_rendererData.deviceQ.transferQueue, _TRSCopyCmdBuffer, _TRSCopyFence, _TRSCopySemaphore);
        //_rendererData.CopyBuffer(_batcher.Trs, GetTRSBuffer(), _TRSCopyFence, _TRSCopySemaphore, _TRSCopyCmdBuffer);

        AddSemaphore(_TRSCopySemaphore);
        SetRenders(_batcher.GetRendGroups());
    }
}
