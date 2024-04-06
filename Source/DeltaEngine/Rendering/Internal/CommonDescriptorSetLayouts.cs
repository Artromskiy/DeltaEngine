using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering.Internal;
internal class CommonDescriptorSetLayouts : IDisposable
{
    private readonly RenderBase _data;
    public DescriptorSetLayout Instance { get; private set; }
    public DescriptorSetLayout Scene { get; private set; }
    public DescriptorSetLayout Material { get; private set; }

    private readonly DescriptorSetLayout[] _layouts;
    public ReadOnlySpan<DescriptorSetLayout> Layouts => _layouts;


    public unsafe CommonDescriptorSetLayouts(RenderBase data)
    {
        _data = data;

        Instance = CreateDescriptorSetLayout(data, [MatricesBindings, IdsBindings]);
        Scene = CreateDescriptorSetLayout(data, [CameraBindings]);
        Material = CreateDescriptorSetLayout(data, [MaterialBindings]);

        _layouts = new DescriptorSetLayout[RendConst.SetsCount];

        _layouts[RendConst.InsSet] = Instance;
        _layouts[RendConst.ScnSet] = Scene;
        _layouts[RendConst.MatSet] = Material;
    }


    public static unsafe DescriptorSetLayout CreateDescriptorSetLayout(RenderBase data, ReadOnlySpan<DescriptorSetLayoutBinding> bindingsArray)
    {
        var bindings = stackalloc DescriptorSetLayoutBinding[bindingsArray.Length];
        bindingsArray.CopyTo(bindings);
        DescriptorSetLayoutCreateInfo createInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = (uint)bindingsArray.Length,
            PBindings = bindings,
        };
        _ = data.vk.CreateDescriptorSetLayout(data.deviceQ, &createInfo, null, out DescriptorSetLayout setLayout);
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
            _data.vk.DestroyDescriptorSetLayout(_data.deviceQ, item, null);
    }
}