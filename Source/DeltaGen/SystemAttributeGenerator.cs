using DeltaGen.Attributes;
using Microsoft.CodeAnalysis;
using DeltaGenCore;

namespace DeltaGen;

[Generator]
internal class SystemAttributeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(OnPostInitOutput);
    }

    private void OnPostInitOutput(IncrementalGeneratorPostInitializationContext ctx)
    {
        //ctx.AddSource(new HelloWorld());
        ctx.AddSource(new SystemAttribute());
        ctx.AddSource(new SystemCallAttribute());

        ctx.AddSource(new AllAttribute());
        ctx.AddSource(new AnyAttribute());
        ctx.AddSource(new NoneAttribute());
        ctx.AddSource(new OnlyAttribute());
    }
}
