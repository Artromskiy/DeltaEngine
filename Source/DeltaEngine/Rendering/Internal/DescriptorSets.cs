using Delta.ECS.Components;
using Delta.Rendering.Collections;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Delta.Rendering.Internal;

internal class DescriptorSets : IDisposable
{
    private readonly Vk _vk;
    private readonly DeviceQueues _deviceQ;
    private readonly DescriptorPool _descriptorPool;
    public readonly SimpleShaderLayouts descriptorSetLayouts;

    private readonly List<(Render rend, int count)> _renderList = [];

    private readonly BindedDynamicBuffer _matrices;
    private readonly BindedDynamicBuffer _ids;
    private readonly BindedDynamicBuffer _materials;
    private readonly BindedDynamicBuffer _camera;

    private readonly DescriptorSet[] _descriptorSets;

    private DescriptorSet Scene => _descriptorSets[RendConst.ScnSet];
    private DescriptorSet Material => _descriptorSets[RendConst.MatSet];
    private DescriptorSet Instance => _descriptorSets[RendConst.InsSet];

    public ReadOnlySpan<(Render rend, int count)> RenderList => CollectionsMarshal.AsSpan(_renderList);
    public DynamicBuffer Camera => _camera;
    public DynamicBuffer Materials => _materials;
    public DynamicBuffer Matrices => _matrices;
    public DynamicBuffer Ids => _ids;

    public DescriptorSets(Vk vk, DeviceQueues deviceQ, DescriptorPool descriptorPool, SimpleShaderLayouts descriptorSetLayouts)
    {
        _vk = vk;
        _deviceQ = deviceQ;
        _descriptorPool = descriptorPool;
        this.descriptorSetLayouts = descriptorSetLayouts;
        int setsCount = descriptorSetLayouts.Layouts.Length;
        _descriptorSets = new DescriptorSet[setsCount];
        for (uint i = 0; i < setsCount; i++)
            _descriptorSets[i] = CreateDescriptorSet(i);

        _matrices = new(_vk, _deviceQ, Instance, RendConst.MatricesBinding, DescriptorType.StorageBuffer);
        _ids = new(_vk, _deviceQ, Instance, RendConst.IdsBinding, DescriptorType.StorageBuffer);
        _materials = new(_vk, _deviceQ, Material, RendConst.MaterialBinding, DescriptorType.StorageBuffer);
        _camera = new(_vk, _deviceQ, Scene, RendConst.CameraBinding, DescriptorType.StorageBuffer);
    }

    public void CopyBatcherData(IRenderBatcher batcher, CommandBuffer copyCmdBuffer)
    {
        _renderList.Clear();
        _renderList.AddRange(batcher.RendGroups);
        RenderHelper.CopyCmd(_vk, batcher.Transforms, Matrices, copyCmdBuffer);
        RenderHelper.CopyCmd(_vk, batcher.TransformIds, Ids, copyCmdBuffer);
        RenderHelper.CopyCmd(_vk, batcher.Camera, Camera, copyCmdBuffer);
    }

    private unsafe DescriptorSet CreateDescriptorSet(uint setId)
    {
        DescriptorSetLayout setLayout = descriptorSetLayouts.Layouts[(int)setId];
        DescriptorSetAllocateInfo allocateInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &setLayout,
        };
        _ = _vk.AllocateDescriptorSets(_deviceQ, allocateInfo, out var result);
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
        _vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, descriptorSetLayouts.pipelineLayout, 0, _descriptorSets, []);
    }

    public unsafe void Dispose()
    {
        _vk.FreeDescriptorSets(_deviceQ, _descriptorPool, _descriptorSets);

        _vk.DestroyBuffer(_deviceQ, _matrices, null);
        _vk.DestroyBuffer(_deviceQ, _ids, null);
        _vk.DestroyBuffer(_deviceQ, _materials, null);
        _vk.DestroyBuffer(_deviceQ, _camera, null);
    }

}