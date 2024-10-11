using GLCSharp.Attributes;
using DeltaGenCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;

namespace GLCSharp
{
    [Generator]
    public sealed class PartialShaderGenerator : IIncrementalGenerator
    {

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var attributeName = new ShaderAttribute().ShortName;
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
                    ctx.ReportNotPartial(nonPartial!.Identifier.GetLocation(), nameof(ShaderAttribute));
                    continue;
                }

                var symbol = compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type)!;
                ShaderTemplate template = new(new(symbol));
                ctx.AddSource(template);
            }
        }
    }
}
