using Silk.NET.Vulkan;

namespace Delta.Rendering.Internal;
internal class BindedDynamicBuffer : DynamicBuffer
{
    private readonly DescriptorSet _descriptorSet;
    private readonly uint _binding;
    private readonly DescriptorType _descriptorType;

    public BindedDynamicBuffer(RenderBase renderBase, DescriptorSet descriptorSet, uint binding) : base(renderBase, 1)
    {
        _descriptorSet = descriptorSet;
        _binding = binding;
    }

    public void UpdateDescriptorSet()
    {
        if (ChangedBuffer)
        {
            RenderHelper.UpdateDescriptorSets(_renderBase, _descriptorSet, this, _binding, DescriptorType.StorageBuffer);
            ChangedBuffer = false;
        }
    }
}
