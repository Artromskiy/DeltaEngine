using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace DeltaGen.Core;
internal static class Extensions
{
    public static void AddSource(this IncrementalGeneratorPostInitializationContext ctx, Template template)
    {
        string fileName = template.Name;
        string templateString = template.ToString();
        string fileContent = CSharpSyntaxTree.ParseText(templateString).GetRoot().NormalizeWhitespace().ToFullString();
        ctx.AddSource(fileName, SourceText.From(fileContent, Encoding.UTF8));
    }


    public static void AddSource(this SourceProductionContext ctx, Template template)
    {
        string fileName = template.Name;
        string templateString = template.ToString();
        string fileContent = CSharpSyntaxTree.ParseText(templateString).GetRoot().NormalizeWhitespace().ToFullString();
        ctx.AddSource(fileName, SourceText.From(fileContent, Encoding.UTF8));
    }

    public static string ToFullDisplayName(this ITypeSymbol typeSymbol) => typeSymbol.ToDisplayString(NullableFlowState.None, SymbolDisplayFormat.FullyQualifiedFormat);


    public static string TypeModifiers(this INamedTypeSymbol symbol)
    {
        return symbol.TypeKind switch
        {
            TypeKind.Class => symbol.IsRecord ? "record" : "class",
            TypeKind.Struct => symbol.IsRecord ? "record struct" : "struct",
            _ => throw new System.Exception(),
        };
    }

    public static string ToDisplayString(this RefKind symbol) => symbol == RefKind.None ? string.Empty : symbol == RefKind.In ? "in" : "ref";

    public static RefKind ValidParameterModifier(this INamedTypeSymbol symbol)
    {
        return symbol.TypeKind switch
        {
            TypeKind.Class => RefKind.None,
            TypeKind.Struct => symbol.IsReadOnly? RefKind.RefReadOnly: RefKind.Ref,
            _ => throw new System.Exception(),
        };
    }
}
