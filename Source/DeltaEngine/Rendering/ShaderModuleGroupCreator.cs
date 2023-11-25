using Silk.NET.Vulkan;
using System;
using System.Runtime.InteropServices;

namespace DeltaEngine.Rendering;

public static class ShaderModuleGroupCreator
{
    private const string ShaderEntryName = "main";
    private static readonly nint namePtr;

    static ShaderModuleGroupCreator()
    {
        namePtr = Marshal.StringToHGlobalAnsi(ShaderEntryName);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Dispose();
    }
    
    private static void Dispose() => Marshal.FreeHGlobal(namePtr);

    public static unsafe PipelineShaderStageCreateInfo Create(PipelineShader shader) => new()
    {
        SType = StructureType.PipelineShaderStageCreateInfo,
        Module = shader.module,
        PName = (byte*)namePtr,
        Stage = shader.stage,
    };
}

