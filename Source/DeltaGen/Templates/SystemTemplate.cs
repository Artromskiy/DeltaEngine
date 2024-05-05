using DeltaGen.Core;
using DeltaGen.Models;
using System.Linq;

namespace DeltaGen.Templates;

internal class SystemTemplate(SystemModel model) : Template<SystemModel>(model)
{
    public override string Name => Model.Name;
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
    
    {{Loop(Model.SystemCalls.Select(m => new SystemCallQueryTemplate(m)))}}
}

{{Model.TypeAccessability}} partial {{Model.TypeDefinition}} {{Model.TypeName}}
{
    public ReadOnlySpan<Type> MutTypes => {{Model.TypeFileName}}.mutTypes;
    public ReadOnlySpan<Type> ReadTypes => {{Model.TypeFileName}}.readTypes;

    public void Update(World world)
    {
        {{Loop(Model.SystemCalls.Select(m => new SystemCallInvokeTemplate(m, "world")))}}
    }
    {{Loop(Model.SystemCalls.Select(m => new SystemCallTemplate(m)))}}
}
""";
}
