using Microsoft.CodeAnalysis;
using DeltaGenCore;
using GLCSharp.Attributes;

namespace GLCSharp;

[Generator]
public class AttributesGenerator : GeneratorBase
{
    public override void Generate(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(OnPostInitOutput);
    }

    private void OnPostInitOutput(IncrementalGeneratorPostInitializationContext ctx)
    {
        ctx.AddSource(new ShaderAttribute());
        ctx.AddSource(new LayoutAttribute());
        ctx.AddSource(new VertexEntryAttribute());
        ctx.AddSource(new FragmentEntryAttribute());
    }
}
