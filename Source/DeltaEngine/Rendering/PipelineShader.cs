using Silk.NET.Vulkan;
using System;
using System.IO;


namespace Delta.Rendering;

internal readonly struct PipelineShader : IDisposable
{
    public readonly ShaderModule module;
    public readonly ShaderStageFlags stage;

    private readonly Vk _vk;
    private readonly Device _device;

    public unsafe PipelineShader(RenderBase data, ShaderStageFlags stage, ReadOnlySpan<byte> shaderCode)
    {
        _vk = data.vk;
        _device = data.deviceQ.device;
        this.stage = stage;

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


    public unsafe void Dispose()
    {
        _vk.DestroyShaderModule(_device, module, null);
    }
}