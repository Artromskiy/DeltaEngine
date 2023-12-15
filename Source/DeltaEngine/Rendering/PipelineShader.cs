using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using Silk.NET.Vulkan;
using System;
using System.IO;


namespace DeltaEngine.Rendering;

public readonly struct PipelineShader : IDisposable
{
    public readonly ShaderModule module;
    public readonly ShaderStageFlags stage;

    public readonly VertexAttribute attributeMask;

    private readonly Vk _vk;
    private readonly Device _device;

    public unsafe PipelineShader(RenderBase data, ShaderStageFlags stage, byte[] shaderCode)
    {
        _vk = data.vk;
        _device = data.deviceQueues.device;
        this.stage = stage;
        if (stage == ShaderStageFlags.VertexBit)
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

    public PipelineShader(RenderBase data, ShaderStageFlags stage, string path) : this(data, stage, File.ReadAllBytes(path)) { }
    private unsafe VertexAttribute GetInputAttributes(byte[] shaderCode)
    {
        Context* context = default;
        ParsedIr* ir = default;
        Compiler* compiler = default;
        Resources* resources = default;
        ReflectedResource* list = default;
        Set set = default;
        nuint count;
        nuint i;
        VertexAttribute res = default;

        using Cross api = Cross.GetApi();
        api.ContextCreate(&context);

        uint[] decoded = new uint[shaderCode.Length / 4];
        System.Buffer.BlockCopy(shaderCode, 0, decoded, 0, shaderCode.Length);
        fixed (uint* decodedPtr = decoded)
        {
            api.ContextParseSpirv(context, decodedPtr, (uint)decoded.Length, &ir);
            api.ContextCreateCompiler(context, Backend.None, ir, CaptureMode.TakeOwnership, &compiler);
            api.CompilerGetActiveInterfaceVariables(compiler, &set);
            api.CompilerCreateShaderResources(compiler, &resources);
            api.ResourcesGetResourceListForType(resources, ResourceType.StageInput, &list, &count);
            for (i = 0; i < count; i++)
            {
                var loc = (int)api.CompilerGetDecoration(compiler, list[i].Id, Decoration.Location);
                res |= (VertexAttribute)(1 << loc);
                var binding = api.CompilerGetDecoration(compiler, list[i].Id, Decoration.Binding);
                var dset = api.CompilerGetDecoration(compiler, list[i].Id, Decoration.DescriptorSet);
            }
        }
        api.ContextDestroy(context);
        return res;
    }

    public unsafe void Dispose()
    {
        _vk.DestroyShaderModule(_device, module, null);
    }
}