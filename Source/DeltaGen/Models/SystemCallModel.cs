using DeltaGen.Attributes;
using DeltaGenCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DeltaGen.Models;

internal record SystemCallModel : Model
{
    public SystemCallModel(IMethodSymbol methodSymbol, SystemModel containingType)
    {
        MethodSymbol = methodSymbol;
        System = containingType;
    }
    public IMethodSymbol MethodSymbol { get; }
    public SystemModel System { get; }
    public ImmutableArray<IParameterSymbol> Parameters => MethodSymbol.Parameters;
    public IEnumerable<string> MutParametersTypes => Parameters.Where(p => p.RefKind == RefKind.Ref).Select(p => p.Type.ToDisplayString());
    public IEnumerable<string> ReadParametersTypes => Parameters.Where(p => p.RefKind != RefKind.Ref).Select(p => p.Type.ToDisplayString());
    public IEnumerable<string> ArgumentModifiers => Parameters.Select(s => s.RefKind.ToDisplayString());
    public IEnumerable<string> ParametersTypes => Parameters.Select(p => p.Type.ToDisplayString());
    public IEnumerable<int> ParametersIndices => Enumerable.Range(0, Parameters.Length);

    private bool HasAllDescription => MethodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(AllAttribute));
    private bool HasAnyDescription => MethodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(AnyAttribute));
    private bool HasNoneDescription => MethodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(NoneAttribute));
    private bool HasOnlyDescription => MethodSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(OnlyAttribute));

    public IEnumerable<string>? AllDescription => !HasAllDescription ?
        ParametersTypes.Distinct() :
        ParametersTypes.Concat(GetAttributeGenericArguments(nameof(AllAttribute)))?.Distinct();
    public IEnumerable<string>? AnyDescription => GetAttributeGenericArguments(nameof(AnyAttribute))?.Distinct();
    public IEnumerable<string>? NoneDescription => GetAttributeGenericArguments(nameof(NoneAttribute))?.Distinct();
    public IEnumerable<string>? OnlyDescription => GetAttributeGenericArguments(nameof(OnlyAttribute))?.Distinct();

    private AttributeData? GetAttribute(string name) => MethodSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == name);
    private IEnumerable<string>? GetAttributeGenericArguments(string name) => GetAttribute(name)?.AttributeClass?.TypeParameters.Select(static s => s.ToDisplayString());

    public int MethodOrder => System.SystemCalls.IndexOf(this);

    public string MethodName => MethodSymbol.Name;
    public string? SystemCallMethodName => $"__{MethodSymbol.Name}Call{MethodOrder}";
    public string? SystemCallQueryName => $"__queryDescription{MethodSymbol.Name}{MethodOrder}";

    public string? QueryIntialization => "";

    public string? SystemCallMethodInput { get; }

    public override string Name { get; set; } = string.Empty;
}