using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering.Internal;
internal class CommonDescriptorSetLayouts : IDisposable
{
    public DescriptorSetLayout Scene { get; private set; }
    public DescriptorSetLayout Material { get; private set; }
    public DescriptorSetLayout Instance { get; private set; }

    public DescriptorSetLayoutBinding[] SceneBindings { get; private set; }
    public DescriptorSetLayoutBinding[] MaterialBindings { get; private set; }
    public DescriptorSetLayoutBinding[] InstanceBindings { get; private set; }

    private readonly DescriptorSetLayout[] _layouts;
    public ReadOnlySpan<DescriptorSetLayout> Layouts => _layouts;

    public unsafe CommonDescriptorSetLayouts(RenderBase data)
    {
        SceneBindings =
        [
            CameraBindings
        ];
        MaterialBindings =
        [

        ];
        InstanceBindings =
        [
            MatricesBindings,
            IdsBindings
        ];
        Scene = CreateDescriptorSetLayout(data, SceneBindings);
        Material = CreateDescriptorSetLayout(data, MaterialBindings);
        Instance = CreateDescriptorSetLayout(data, InstanceBindings);
        _layouts = [Instance, Scene, Material];
    }


    public static unsafe DescriptorSetLayout CreateDescriptorSetLayout(RenderBase data, DescriptorSetLayoutBinding[] bindingsArray)
    {
        var bindings = stackalloc DescriptorSetLayoutBinding[bindingsArray.Length];
        bindingsArray.CopyTo(bindings);
        DescriptorSetLayoutCreateInfo createInfo = new()
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = (uint)bindingsArray.Length,
            PBindings = bindings,
        };
        _ = data.vk.CreateDescriptorSetLayout(data.deviceQ.device, &createInfo, null, out DescriptorSetLayout setLayout);
        return setLayout;
    }


    private static readonly ShaderStageFlags StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit;

    private static readonly unsafe DescriptorSetLayoutBinding MatricesBindings = new(0, DescriptorType.StorageBuffer, 1, StageFlags);
    private static readonly unsafe DescriptorSetLayoutBinding IdsBindings = new(1, DescriptorType.StorageBuffer, 1, StageFlags);

    private static readonly unsafe DescriptorSetLayoutBinding CameraBindings = new(0, DescriptorType.UniformBuffer, 1, StageFlags);

    public void Dispose() => throw new NotImplementedException();
}
