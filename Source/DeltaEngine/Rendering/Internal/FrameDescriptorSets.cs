using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering.Internal;

internal class FrameDescriptorSets: IDisposable
{
    private readonly RenderBase _renderBase;

    private readonly BindedDynamicBuffer _matrices;
    private readonly BindedDynamicBuffer _ids;
    private readonly BindedDynamicBuffer _materials;
    private readonly BindedDynamicBuffer _camera;

    private readonly DescriptorSet[] _descriptorSets;

    private DescriptorSet Instance=> _descriptorSets[RendConst.InsSet];
    private DescriptorSet Scene => _descriptorSets[RendConst.ScnSet];
    private DescriptorSet Material => _descriptorSets[RendConst.MatSet];

    public DynamicBuffer Matrices => _matrices;
    public DynamicBuffer Ids => _ids;
    public DynamicBuffer Materials => _materials;
    public DynamicBuffer Camera => _camera;

    private CommonDescriptorSetLayouts SetLayouts => _renderBase.descriptorSetLayouts;

    public FrameDescriptorSets(RenderBase renderBase)
    {
        _renderBase = renderBase;

        _descriptorSets = new DescriptorSet[RendConst.SetsCount];
        for (uint i = 0; i < RendConst.SetsCount; i++)
            _descriptorSets[i] = CreateDescriptorSet(i);

        _matrices = new(_renderBase, Instance, RendConst.MatricesBinding);
        _ids = new(_renderBase, Instance, RendConst.IdsBinding);
        _materials = new(_renderBase, Material, RendConst.MaterialBinding);
        _camera = new(_renderBase, Scene, RendConst.CameraBinding);
    }

    private unsafe DescriptorSet CreateDescriptorSet(uint setId)
    {
        DescriptorSetLayout setLayout = SetLayouts.Layouts[(int)setId];
        DescriptorSetAllocateInfo allocateInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _renderBase.descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &setLayout,
        };
        _ = _renderBase.vk.AllocateDescriptorSets(_renderBase.deviceQ, allocateInfo, out var result);
        return result;
    }

    public void UpdateDescriptorSets()
    {
        _camera.UpdateDescriptorSet();
        _materials.UpdateDescriptorSet();
        _matrices.UpdateDescriptorSet();
        _ids.UpdateDescriptorSet();
    }

    public unsafe void BindDescriptorSets(CommandBuffer commandBuffer)
    {
        for (uint i = 0; i < RendConst.SetsCount; i++)
        {
            var set = _descriptorSets[i];
            _renderBase.vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _renderBase.pipelineLayout, i, 1, set, 0, 0);
        }

    }

    public unsafe void Dispose()
    {
        _renderBase.vk.FreeDescriptorSets(_renderBase.deviceQ, _renderBase.descriptorPool, _descriptorSets);

        _renderBase.vk.DestroyBuffer(_renderBase.deviceQ, _matrices, null);
        _renderBase.vk.DestroyBuffer(_renderBase.deviceQ, _ids, null);
        _renderBase.vk.DestroyBuffer(_renderBase.deviceQ, _materials, null);
        _renderBase.vk.DestroyBuffer(_renderBase.deviceQ, _camera, null);

    }
}