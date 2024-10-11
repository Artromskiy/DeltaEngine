using Microsoft.CodeAnalysis;
using DeltaGenCore;
using GLCSharp.Attributes;

namespace GLCSharp;

[Generator]
public class AttributesGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(OnPostInitOutput);
    }
    private void OnPostInitOutput(IncrementalGeneratorPostInitializationContext ctx)
    {
        ctx.AddSource(new ShaderAttribute());
    }
}
