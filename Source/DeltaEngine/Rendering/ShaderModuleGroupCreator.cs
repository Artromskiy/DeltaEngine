using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace DeltaEngine.Rendering;

public ref struct ShaderModuleGroupCreator
{
    private const string entryName = "main";
    private readonly nint namePtr;

    public unsafe ShaderModuleGroupCreator()
    {
        namePtr = SilkMarshal.StringToPtr(entryName);
    }

    public unsafe PipelineShaderStageCreateInfo Create(Shader shader)
    {
        return new()
        {
            Module = shader.module,
            PName = (byte*)namePtr,
            Stage = shader.stage,
        };
    }

    public readonly void Dispose()
    {
        SilkMarshal.Free(namePtr);
    }
}
