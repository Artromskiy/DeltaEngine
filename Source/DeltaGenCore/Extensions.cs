using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using System.Text;

namespace DeltaGenCore;
public static class Extensions
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
            _ => throw new Exception(),
        };
    }

    public static string TypeDeclaration(this INamedTypeSymbol symbol)
    {
        return $"{SyntaxFacts.GetText(symbol.DeclaredAccessibility)} partial {symbol.TypeModifiers()} {symbol.Name}";
    }

    public static bool IsAllContentsPartial(this INamedTypeSymbol symbol)
    {
        var current = symbol;
        while (current != null)
        {
            if (!symbol.ToFullDisplayName().Contains("partial "))
                return false;
            current = symbol.ContainingType;
        }
        return true;
    }

    public static bool IsAllPartialToRoot(this BaseTypeDeclarationSyntax syntax, out BaseTypeDeclarationSyntax? nonPartial)
    {
        nonPartial = syntax;
        while (nonPartial != null)
        {
            if (!nonPartial.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                return false;

            var parent = syntax.Parent as BaseTypeDeclarationSyntax;
            nonPartial = parent == nonPartial ? null : parent;
        }
        return true;
    }

    public static string ToDisplayString(this RefKind symbol) => symbol == RefKind.None ? string.Empty : symbol == RefKind.In ? "in" : "ref";

    public static RefKind ValidParameterModifier(this INamedTypeSymbol symbol)
    {
        return symbol.TypeKind switch
        {
            TypeKind.Class => RefKind.None,
            TypeKind.Struct => symbol.IsReadOnly ? RefKind.RefReadOnly : RefKind.Ref,
            _ => throw new Exception(),
        };
    }
}
