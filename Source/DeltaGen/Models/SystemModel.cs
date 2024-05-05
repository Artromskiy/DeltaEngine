using DeltaGen.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DeltaGen.Models;
internal record SystemModel : Model
{
    public SystemModel(INamedTypeSymbol typeSymbol, string attributeSearch)
    {
        TypeSymbol = typeSymbol;
        SystemCalls = typeSymbol.GetMembers().OfType<IMethodSymbol>().
            Where(x => x.GetAttributes().Any(a => a.AttributeClass!.Name == attributeSearch)).
            Select(m => new SystemCallModel(m, this)).ToImmutableArray();
    }
    public ImmutableArray<SystemCallModel> SystemCalls { get; }
    public INamedTypeSymbol TypeSymbol { get; }
    public string TypeNamespace => TypeSymbol.ContainingNamespace.ToDisplayString();
    public string TypeDefinition => TypeSymbol.TypeModifiers();
    public string TypeAccessability => SyntaxFacts.GetText(TypeSymbol.DeclaredAccessibility);
    public IEnumerable<string> MutTypes => SystemCalls.SelectMany(s => s.MutParametersTypes).Distinct();
    public IEnumerable<string> ReadTypes => SystemCalls.SelectMany(s => s.ReadParametersTypes).Distinct();
    public IEnumerable<string> Namespaces => SystemCalls.SelectMany(s => s.Parameters.Select(s=>s.Type.ContainingNamespace.ToDisplayString())).Distinct();
    public RefKind ParameterModifier => TypeSymbol.ValidParameterModifier();
    public string ParameterModifierString => TypeSymbol.ValidParameterModifier().ToDisplayString();
    public string TypeName => TypeSymbol.Name;
    public string TypeFileName => $"{TypeSymbol.Name}File";
    public override string Name
    {
        get => TypeSymbol.Name;
        set => _ = value;
    }
}
