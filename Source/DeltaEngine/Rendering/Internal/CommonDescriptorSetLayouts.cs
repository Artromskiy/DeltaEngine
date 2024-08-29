using Delta.Utilities;
using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering.Internal;
internal class CommonDescriptorSetLayouts : IDisposable
{
    private readonly Vk _vk;
    private readonly DeviceQueues _deviceQ;
    public DescriptorSetLayout Instance { get; private set; }
    public DescriptorSetLayout Scene { get; private set; }
    public DescriptorSetLayout Material { get; private set; }

    private readonly DescriptorSetLayout[] _layouts;
    public ReadOnlySpan<DescriptorSetLayout> Layouts => _layouts;

    public unsafe CommonDescriptorSetLayouts(Vk vk, DeviceQueues deviceQ)
    {
        _vk = vk;
        _deviceQ = deviceQ;

        Instance = CreateDescriptorSetLayout([MatricesBindings, IdsBindings]);
        Scene = CreateDescriptorSetLayout([CameraBindings]);
        Material = CreateDescriptorSetLayout([MaterialBindings]);

        _layouts = new DescriptorSetLayout[RendConst.SetsCount];

        _layouts[RendConst.InsSet] = Instance;
        _layouts[RendConst.ScnSet] = Scene;
        _layouts[RendConst.MatSet] = Material;
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

    private const ShaderStageFlags StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit;

    private static readonly unsafe DescriptorSetLayoutBinding MatricesBindings = new(RendConst.MatricesBinding, DescriptorType.StorageBuffer, 1, StageFlags);
    private static readonly unsafe DescriptorSetLayoutBinding IdsBindings = new(RendConst.IdsBinding, DescriptorType.StorageBuffer, 1, StageFlags);
    private static readonly unsafe DescriptorSetLayoutBinding CameraBindings = new(RendConst.CameraBinding, DescriptorType.StorageBuffer, 1, StageFlags);
    private static readonly unsafe DescriptorSetLayoutBinding MaterialBindings = new(RendConst.MaterialBinding, DescriptorType.StorageBuffer, 1, StageFlags);

    public unsafe void Dispose()
    {
        foreach (var item in _layouts)
            _vk.DestroyDescriptorSetLayout(_deviceQ, item, null);
    }
}