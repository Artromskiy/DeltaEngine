using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering.Internal;

internal class DynamicDescriptorSets : IDisposable
{
    private readonly IRenderBatcher _batcher;

    private readonly Vk _vk;
    private readonly DeviceQueues _deviceQ;
    private readonly DescriptorPool _descriptorPool;
    private readonly DescriptorSet[] _descriptorSets;

    private readonly BindedDynamicBuffer[] _buffers;

    public DynamicDescriptorSets(Vk vk, DeviceQueues deviceQ, DescriptorPool descriptorPool, IRenderBatcher batcher)
    {
        _vk = vk;
        _deviceQ = deviceQ;
        _descriptorPool = descriptorPool;
        _batcher = batcher;

        int setsCount = _batcher.Layouts.Length;
        _descriptorSets = new DescriptorSet[setsCount];
        for (int i = 0; i < setsCount; i++)
            _descriptorSets[i] = CreateDescriptorSet(_batcher.Layouts[i]);

        int buffersCount = _batcher.Buffers.Length;
        _buffers = new BindedDynamicBuffer[buffersCount];
        for (int i = 0; i < buffersCount; i++)
            _buffers[i] = new(_vk, _deviceQ, _descriptorSets[i], _batcher.Bindings[i], DescriptorType.StorageBuffer);
    }


    public void CopyBatcherData(CommandBuffer copyCmdBuffer)
    {
        for (int i = 0; i < _batcher.Buffers.Length; i++)
            RenderHelper.CopyCmd(_vk, _batcher.Buffers[i], _buffers[i], copyCmdBuffer);
    }

    public void UpdateDescriptorSets()
    {
        foreach (var buffer in _buffers)
            buffer.UpdateDescriptorSet();
    }

    public unsafe void BindDescriptorSets(CommandBuffer commandBuffer)
    {
        _vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, _batcher.PipelineLayout, 0, _descriptorSets, []);
    }

    private unsafe DescriptorSet CreateDescriptorSet(DescriptorSetLayout layout)
    {
        DescriptorSetAllocateInfo allocateInfo = new()
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout,
        };
        _ = _vk.AllocateDescriptorSets(_deviceQ, allocateInfo, out var result);
        return result;
    }

    public unsafe void Dispose()
    {
        _vk.FreeDescriptorSets(_deviceQ, _descriptorPool, _descriptorSets);

        foreach (var buffer in _buffers)
            _vk.DestroyBuffer(_deviceQ, buffer, null);
    }
}
