using Delta.Files;
using Silk.NET.SPIRV;
using Silk.NET.SPIRV.Cross;
using System;
using System.Collections.Immutable;

namespace Delta.Rendering;

public class ShaderData : IAsset
{
    public readonly VertexAttribute vertexMask;
    public readonly ImmutableArray<byte> vertBytes;
    public readonly ImmutableArray<byte> fragBytes;

    public ShaderData(byte[] vert, byte[] frag)
    {
        vertBytes = ImmutableArray.Create(vert);
        fragBytes = ImmutableArray.Create(frag);
        vertexMask = GetInputAttributes(vertBytes.AsSpan());
    }
    private ShaderData() { }
    public static ShaderData DummyShaderData()
    {
        return new ShaderData();
    }

    private unsafe VertexAttribute GetInputAttributes(ReadOnlySpan<byte> shaderCode)
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