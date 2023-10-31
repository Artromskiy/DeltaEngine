using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using System;
using System.Collections.Immutable;
using System.IO;

namespace DeltaEngine.Rendering;
internal struct Shader
{
    public VertexAttribute vertexMask;
    public string vert;
    public string frag;

    public readonly ImmutableArray<byte> vertBytes;
    public readonly ImmutableArray<byte> fragBytes;

    public Shader(string fragPath, string vertPath)
    {
        frag = fragPath;
        vert = vertPath;
        vertBytes = ImmutableArray.Create(File.ReadAllBytes(vert));
        fragBytes = ImmutableArray.Create(File.ReadAllBytes(frag));
        vertexMask = GetInputAttributes(vertBytes.AsSpan());
    }

    private readonly unsafe VertexAttribute GetInputAttributes(ReadOnlySpan<byte> shaderCode)
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

        fixed (byte* decodedPtr = shaderCode)
        {
            api.ContextParseSpirv(context, (uint)decodedPtr, (uint)shaderCode.Length / 4, &ir);
            api.ContextCreateCompiler(context, Backend.None, ir, CaptureMode.TakeOwnership, &compiler);
            api.CompilerGetActiveInterfaceVariables(compiler, &set);
            api.CompilerCreateShaderResourcesForActiveVariables(compiler, &resources, &set);
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
