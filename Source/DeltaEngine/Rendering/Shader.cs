using Silk.NET.Vulkan;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Vortice.SpirvCross;
using static DeltaEngine.ThrowHelper;
namespace DeltaEngine.Rendering;

public readonly struct Shader : IDisposable
{
    public readonly ShaderModule module;
    public readonly ShaderStageFlags stage;

    public readonly VertexAttribute attributeMask;

    private readonly Vk _vk;
    private readonly Device _device;

    public unsafe Shader(RenderBase data, ShaderStageFlags stage, byte[] shaderCode)
    {
        _vk = data.vk;
        _device = data.device;
        this.stage = stage;

        if(stage == ShaderStageFlags.VertexBit)
            attributeMask = GetInputAttributes(shaderCode);

        fixed (byte* code = shaderCode)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)shaderCode.Length,
                PCode = (uint*)code,
            };
            _ = _vk.CreateShaderModule(_device, createInfo, null, out module);
        }
    }

    public Shader(RenderBase data, ShaderStageFlags stage, string path) : this(data, stage, File.ReadAllBytes(path)) { }
    private unsafe VertexAttribute GetInputAttributes(byte[] shaderCode)
    {
        spvc_context context = default;
        spvc_parsed_ir ir = default;
        spvc_compiler compiler = default;
        spvc_resources resources = default;
        spvc_reflected_resource* list = default;
        spvc_set set = default;
        nuint count;
        nuint i;
        VertexAttribute res = default;


        uint[] decoded = new uint[shaderCode.Length / 4];
        System.Buffer.BlockCopy(shaderCode, 0, decoded, 0, shaderCode.Length);

        SpirvCrossApi.spvc_context_create(&context);
        fixed (uint* decodedPtr = decoded)
        {
            SpirvCrossApi.spvc_context_parse_spirv(context, decodedPtr, (uint)decoded.Length, &ir);
            var api = SpirvCrossApi.spvc_context_create_compiler(context, spvc_backend.SPVC_BACKEND_NONE, ir, spvc_capture_mode.SPVC_CAPTURE_MODE_TAKE_OWNERSHIP, out compiler);
            SpirvCrossApi.spvc_compiler_get_active_interface_variables(compiler, &set);
            SpirvCrossApi.spvc_compiler_create_shader_resources(compiler, &resources);
            SpirvCrossApi.spvc_resources_get_resource_list_for_type(resources, spvc_resource_type.SPVC_RESOURCE_TYPE_STAGE_INPUT, &list, &count);
            for (i = 0; i < count; i++)
            {
                var loc = (int)SpirvCrossApi.spvc_compiler_get_decoration(compiler, list[i].id, Vortice.SPIRV.SpvDecoration.Location);
                res |= (VertexAttribute)(1 << loc);
                var binding = SpirvCrossApi.spvc_compiler_get_decoration(compiler, list[i].id, Vortice.SPIRV.SpvDecoration.Binding);
                var dset = SpirvCrossApi.spvc_compiler_get_decoration(compiler, list[i].id, Vortice.SPIRV.SpvDecoration.DescriptorSet);
            }
        }
        SpirvCrossApi.spvc_context_destroy(context);
        return res;
    }

    public unsafe void Dispose()
    {
        _vk.DestroyShaderModule(_device, module, null);
    }
}