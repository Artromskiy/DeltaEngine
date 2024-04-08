using Arch.Core;
using Delta.ECS;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;


namespace Delta.Rendering;
internal class Renderer : BaseRenderer
{
    private readonly Fence _copyFence;
    private readonly Semaphore _copySemapthore;

    private CommandBuffer _copyCmdBuffer;

    private readonly Batcher _batcher;
    private readonly SceneDataProvider _sceneProvider;

    public Renderer(World world, string appName) : base(appName)
    {
        _copyCmdBuffer = RenderHelper.CreateCommandBuffer(_rendererData, _rendererData.deviceQ.transferCmdPool);
        _copyFence = RenderHelper.CreateFence(_rendererData, true);
        _copySemapthore = RenderHelper.CreateSemaphore(_rendererData);

        _batcher = new Batcher(world, _rendererData);
        _sceneProvider = new SceneDataProvider(world, _rendererData);
    }

    public override void PreSync()
    {
        _rendererData.vk.WaitForFences(_rendererData.deviceQ, 1, _copyFence, true, ulong.MaxValue);
        _rendererData.vk.ResetCommandBuffer(_copyCmdBuffer, 0);

        _batcher.Execute();
        _sceneProvider.Execute();
    }

    public sealed override void PostSync()
    {
        _rendererData.vk.ResetFences(_rendererData.deviceQ, 1, _copyFence);
        _rendererData.BeginCmdBuffer(_copyCmdBuffer);

        _rendererData.CopyCmd(_batcher.trs, DescriptorSets.Matrices, _copyCmdBuffer);
        _rendererData.CopyCmd(_batcher.trsIds, DescriptorSets.Ids, _copyCmdBuffer);
        _rendererData.CopyCmd(_sceneProvider.camera, DescriptorSets.Camera, _copyCmdBuffer);

        _rendererData.EndCmdBuffer(_rendererData.deviceQ.transferQueue, _copyCmdBuffer, _copyFence, _copySemapthore);

        AddSemaphore(_copySemapthore);
        SetRenders(_batcher.GetRendGroups());
    }
}
