using Silk.NET.Vulkan;
using System;
using System.Runtime.InteropServices;

namespace DeltaEngine.Rendering;

public ref struct ShaderModuleGroupCreator
{
    private const string entryName = "main";
    private static readonly nint namePtr;

    static ShaderModuleGroupCreator()
    {
        namePtr = Marshal.StringToHGlobalAnsi(entryName);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Dispose();
    }
    private static void Dispose() => Marshal.FreeHGlobal(namePtr);

    public static unsafe PipelineShaderStageCreateInfo Create(PipelineShader shader)
    {
        return new()
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Module = shader.module,
            PName = (byte*)namePtr,
            Stage = shader.stage,
        };
    }

}
