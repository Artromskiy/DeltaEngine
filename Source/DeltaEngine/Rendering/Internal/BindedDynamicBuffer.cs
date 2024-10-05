using Delta.Rendering.Collections;
using Silk.NET.Vulkan;

namespace Delta.Rendering.Internal;
internal class BindedDynamicBuffer : DynamicBuffer
{
    private readonly DescriptorSet _descriptorSet;
    private readonly uint _binding;
    private readonly DescriptorType _descriptorType;

    public BindedDynamicBuffer(Vk vk, DeviceQueues deviceQ, DescriptorSet descriptorSet, int binding, DescriptorType descriptorType) : base(vk, deviceQ, 1)
    {
        _descriptorSet = descriptorSet;
        _binding = checked((uint)binding);
        _descriptorType = descriptorType;
    }

    public void UpdateDescriptorSet()
    {
        if (ChangedBuffer)
        {
            RenderHelper.UpdateDescriptorSets(_vk, _deviceQ, _descriptorSet, this, _binding, _descriptorType);
            ChangedBuffer = false;
        }
    }
}
