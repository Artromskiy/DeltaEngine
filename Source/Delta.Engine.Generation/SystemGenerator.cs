using Delta.Engine.Generation.Attributes;
using Delta.Engine.Generation.Core;
using Delta.Engine.Generation.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;

namespace Delta.Engine.Generation;

[Generator]
public sealed class SystemGenerator : GeneratorBase
{
    public override void Generate(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(new SystemAttribute());
            ctx.AddSource(new SystemCallAttribute());

            ctx.AddSource(new AllAttribute());
            ctx.AddSource(new AnyAttribute());
            ctx.AddSource(new NoneAttribute());
            ctx.AddSource(new OnlyAttribute());
        });

        var attributeName = new SystemAttribute().ShortName;
        var classDeclarations = context.SyntaxProvider.CreateSyntaxProvider
        (
            (s, _) => IsTypeWithAttribute(s, attributeName),
            static (ctx, _) => ctx.Node as BaseTypeDeclarationSyntax
        );
        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());
        context.RegisterSourceOutput
        (
            compilationAndClasses,
            static (spc, source) => Execute(source.Left, source.Right, spc)
        );
    }

    private static bool IsTypeWithAttribute(SyntaxNode syntaxNode, string attributeName)
    {
        if (syntaxNode is not BaseTypeDeclarationSyntax type)
            return false;
        return type.AttributeLists.Any(l => l.Attributes.Any(a => a.Name.ToFullString() == attributeName));
    }

    private static void Execute(Compilation compilation,
    ImmutableArray<BaseTypeDeclarationSyntax?> types,
    SourceProductionContext ctx)
    {
        foreach (var type in types)
        {
            if (type == null)
                continue;
            if (!type.IsAllPartialToRoot(out var nonPartial))
            {
                ctx.ReportNotPartial(nonPartial!.Identifier.GetLocation(), nameof(SystemAttribute));
                continue;
            }

            var symbol = compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type)!;
            SystemTemplate template = new(new(symbol, nameof(SystemCallAttribute)));
            ctx.AddSource(template);
        }
    }

    private void OnPostInitOutput(IncrementalGeneratorPostInitializationContext ctx)
    {
        //ctx.AddSource(new HelloWorld());
        ctx.AddSource(new SystemAttribute());
        ctx.AddSource(new SystemCallAttribute());

        //ctx.AddSource(new AllAttribute());
        //ctx.AddSource(new AnyAttribute());
        //ctx.AddSource(new NoneAttribute());
        //ctx.AddSource(new OnlyAttribute());
    }
}
