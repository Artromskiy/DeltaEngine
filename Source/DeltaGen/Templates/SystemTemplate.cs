using DeltaGen.Models;
using DeltaGenCore;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaGen.Templates;

internal class SystemTemplate(SystemModel model) : Template<SystemModel>(model)
{
    public override string Name => string.Join(".", Model.ContainingTypes().Select(s => s.Name).Concat([Model.Name]));
    public override string ToString() =>
$$"""
using System;
using Arch.Core;
using System.Runtime.CompilerServices;

namespace {{Model.TypeNamespace}};


file static class {{Model.TypeName}}File
{
    public static Type[] mutTypes = [{{Join("typeof({0})", ", ", Model.MutTypes)}}];
    public static Type[] readTypes = [{{Join("typeof({0})", ", ", Model.ReadTypes)}}];
    
    {{LoopSelect(Model.SystemCalls, m => new SystemCallQueryTemplate(m))}}
}

{{ContainingTypeOpen()}}

{{Model.TypeSymbol.TypeDeclaration()}}
{
    public ReadOnlySpan<Type> MutTypes => {{Model.TypeFileName}}.mutTypes;
    public ReadOnlySpan<Type> ReadTypes => {{Model.TypeFileName}}.readTypes;

    public void Update(World world)
    {
        {{LoopSelect(Model.SystemCalls,m => new SystemCallInvokeTemplate(m, "world"))}}
    }
    {{LoopSelect(Model.SystemCalls, m => new SystemCallTemplate(m))}}
}

{{ContainingTypeClose()}}

""";

    private string ContainingTypeOpen()
    {
        StringBuilder sb = new();
        foreach (var symbol in Model.ContainingTypes())
            sb.Append(symbol.TypeDeclaration()).AppendLine().Append("{").AppendLine();
        return sb.ToString();
    }

    private string ContainingTypeClose()
    {
        StringBuilder sb = new();
        for (int i = 0; i < Model.ContainingTypesCount(); i++)
            sb.AppendLine().Append("}").AppendLine();
        return sb.ToString();
    }
}
