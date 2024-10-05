using DeltaGenInternal.Core;
using DeltaGenInternal.Templates;
using Microsoft.CodeAnalysis;

namespace DeltaGenInternal;

[Generator]
public sealed class BatcherGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
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