using Delta.Rendering;
using Silk.NET.SPIRV.Cross;
using Silk.NET.SPIRV;
using System;

namespace DeltaEditorLib.Compile;
internal static class SpirvCrossHelper
{
    public unsafe static VertexAttribute GetInputAttributes(ReadOnlySpan<byte> shaderCode)
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

        fixed (void* decodedPtr = shaderCode)
        {
            api.ContextParseSpirv(context, (uint*)decodedPtr, (uint)shaderCode.Length / 4, &ir);
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

}
