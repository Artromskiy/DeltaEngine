using DeltaGen.Attributes;
using DeltaGen.Core;
using DeltaGen.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;

namespace DeltaGen;

[Generator]
public sealed class GlobalGen : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(OnPostInitOutput);
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

    private static bool IsTypeWithAttribute(SyntaxNode syntaxNode, string attributeSearch)
    {
        if (syntaxNode.Parent is BaseTypeDeclarationSyntax) // only top level types
            return false;
        if (syntaxNode is not BaseTypeDeclarationSyntax type) // only types
            return false;
        return type.AttributeLists.Any(l => l.Attributes.Any(a => a.Name.ToFullString() == attributeSearch));
    }

    private static void Execute(Compilation compilation,
    ImmutableArray<BaseTypeDeclarationSyntax?> types,
    SourceProductionContext ctx)
    {
        foreach (var type in types)
        {
            if (type == null)
                continue;
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

        ctx.AddSource(new AllAttribute());
        ctx.AddSource(new AnyAttribute());
        ctx.AddSource(new NoneAttribute());
        ctx.AddSource(new OnlyAttribute());
    }
}