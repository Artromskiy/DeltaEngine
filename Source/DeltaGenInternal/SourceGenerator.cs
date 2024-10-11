using DeltaGenCore;
using Microsoft.CodeAnalysis;

namespace DeltaGenInternal;

[Generator]
public sealed class SourceGenerator : GeneratorBase
{
    public override void Generate(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(OnPostInitOutput);
    }


    private void OnPostInitOutput(IncrementalGeneratorPostInitializationContext ctx)
    {
        ctx.AddSource(new GenericBatcher());
        //ctx.AddSource(new HelloWorld());
        //ctx.AddSource(new OnlyAttribute());
    }
}