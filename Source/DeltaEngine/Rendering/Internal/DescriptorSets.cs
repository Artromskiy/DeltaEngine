using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering.Collections;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Delta.Rendering.Internal;

internal class DescriptorSets : IDisposable
{
    private readonly RenderBase _renderBase;

    private readonly List<(Render rend, uint count)> _renderList = [];

    private readonly BindedDynamicBuffer _matrices;
    private readonly BindedDynamicBuffer _ids;
    private readonly BindedDynamicBuffer _materials;
    private readonly BindedDynamicBuffer _camera;

    private readonly DescriptorSet[] _descriptorSets;

    private DescriptorSet Scene => _descriptorSets[RendConst.ScnSet];
    private DescriptorSet Material => _descriptorSets[RendConst.MatSet];
    private DescriptorSet Instance => _descriptorSets[RendConst.InsSet];

    public ReadOnlySpan<(Render rend, uint count)> RenderList => CollectionsMarshal.AsSpan(_renderList);
    public DynamicBuffer Camera => _camera;
    public DynamicBuffer Materials => _materials;
    public DynamicBuffer Matrices => _matrices;
    public DynamicBuffer Ids => _ids;

    private CommonDescriptorSetLayouts SetLayouts => _renderBase.descriptorSetLayouts;

    public DescriptorSets(RenderBase renderBase)
    {
        _renderBase = renderBase;

        _descriptorSets = new DescriptorSet[RendConst.SetsCount];
        for (uint i = 0; i < RendConst.SetsCount; i++)
            _descriptorSets[i] = CreateDescriptorSet(i);

        _matrices = new(_renderBase, Instance, RendConst.MatricesBinding, DescriptorType.StorageBuffer);
        _ids = new(_renderBase, Instance, RendConst.IdsBinding, DescriptorType.StorageBuffer);
        _materials = new(_renderBase, Material, RendConst.MaterialBinding, DescriptorType.StorageBuffer);
        _camera = new(_renderBase, Scene, RendConst.CameraBinding, DescriptorType.StorageBuffer);
    }

    public void CopyBatcherData(IRenderBatcher batcher, CommandBuffer copyCmdBuffer)
    {
        _renderList.Clear();
        _renderList.AddRange(batcher.RendGroups);
        _renderBase.CopyCmd(batcher.Transforms, Matrices, copyCmdBuffer);
        _renderBase.CopyCmd(batcher.TransformIds, Ids, copyCmdBuffer);
        _renderBase.CopyCmd(batcher.Camera, Camera, copyCmdBuffer);
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
            _renderBase.vk.CmdBindDescriptorSets(
                commandBuffer,
                PipelineBindPoint.Graphics,
                _renderBase.pipelineLayout,
                i, 1, _descriptorSets[i], 0, 0);
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