using Silk.NET.Vulkan;
using System;
using System.IO;

namespace Delta.Rendering.Internal;

internal readonly struct PipelineShader : IDisposable
{
    public readonly ShaderModule module;
    public readonly ShaderStageFlags stage;

    private readonly Vk _vk;
    private readonly Device _deviceQ;

    public unsafe PipelineShader(Vk vk, DeviceQueues deviceQ, ShaderStageFlags stage, ReadOnlySpan<byte> shaderCode)
    {
        _vk = vk;
        _deviceQ = deviceQ;
        this.stage = stage;

        fixed (byte* code = shaderCode)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)shaderCode.Length,
                PCode = (uint*)code,
            };
            _ = _vk.CreateShaderModule(_deviceQ, createInfo, null, out module);
        }
    }

    public PipelineShader(Vk vk, DeviceQueues deviceQ, ShaderStageFlags stage, string path) :
        this(vk, deviceQ, stage, File.ReadAllBytes(path))
    { }


    public unsafe void Dispose()
    {
        _vk.DestroyShaderModule(_deviceQ, module, null);
    }
}