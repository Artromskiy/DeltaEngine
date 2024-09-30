using Delta.Utilities;
using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering.Internal;
internal class SimpleShaderLayouts : IDisposable, IShaderLayouts
{
    private const ShaderStageFlags StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit;

    private readonly Vk _vk;
    private readonly DeviceQueues _deviceQ;
    private readonly DescriptorSetLayout[] _layouts;

    public ReadOnlySpan<DescriptorSetLayout> Layouts => _layouts;
    public readonly PipelineLayout pipelineLayout;

    public unsafe SimpleShaderLayouts(Vk vk, DeviceQueues deviceQ)
    {
        _vk = vk;
        _deviceQ = deviceQ;

        _layouts = new DescriptorSetLayout[RendConst.SetsCount];

        _layouts[RendConst.InsSet] = CreateDescriptorSetLayout([MatricesBindings, IdsBindings]);
        _layouts[RendConst.MatSet] = CreateDescriptorSetLayout([CameraBindings]);
        _layouts[RendConst.ScnSet] = CreateDescriptorSetLayout([MaterialBindings]);

        pipelineLayout = CreatePipelineLayout(_vk, _deviceQ, Layouts);
    }

    private unsafe DescriptorSetLayout CreateDescriptorSetLayout(ReadOnlySpan<DescriptorSetLayoutBinding> bindingsArray)
    {
        var bindings = stackalloc DescriptorSetLayoutBinding[bindingsArray.Length];
        bindingsArray.CopyTo(bindings);
        DescriptorSetLayoutCreateInfo createInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = (uint)bindingsArray.Length,
            PBindings = bindings,
        };
        _ = _vk.CreateDescriptorSetLayout(_deviceQ, &createInfo, null, out DescriptorSetLayout setLayout);
        return setLayout;
    }

    private unsafe static PipelineLayout CreatePipelineLayout(Vk vk, Device device, ReadOnlySpan<DescriptorSetLayout> layouts)
    {
        PipelineLayout result;
        fixed (DescriptorSetLayout* layoutsPtr = layouts)
        {
            PipelineLayoutCreateInfo pipelineLayoutInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint)layouts.Length,
                PSetLayouts = layoutsPtr,
            };
            _ = vk.CreatePipelineLayout(device, pipelineLayoutInfo, null, out result);
        }
        return result;
    }


    private static readonly unsafe DescriptorSetLayoutBinding MatricesBindings = new(RendConst.MatricesBinding, DescriptorType.StorageBuffer, 1, StageFlags);
    private static readonly unsafe DescriptorSetLayoutBinding IdsBindings = new(RendConst.IdsBinding, DescriptorType.StorageBuffer, 1, StageFlags);
    private static readonly unsafe DescriptorSetLayoutBinding CameraBindings = new(RendConst.CameraBinding, DescriptorType.StorageBuffer, 1, StageFlags);
    private static readonly unsafe DescriptorSetLayoutBinding MaterialBindings = new(RendConst.MaterialBinding, DescriptorType.StorageBuffer, 1, StageFlags);

    public unsafe void Dispose()
    {
        _vk.DestroyPipelineLayout(_deviceQ, pipelineLayout, null);
        foreach (var item in _layouts)
            _vk.DestroyDescriptorSetLayout(_deviceQ, item, null);
    }
}