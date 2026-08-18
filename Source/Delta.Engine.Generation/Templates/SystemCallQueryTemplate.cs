using Delta.Engine.Generation.Models;
using Delta.Engine.Generation.Core;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

using Delta.Engine.Generation.Templates;
internal class SystemCallQueryTemplate(SystemCallModel model) : Template<SystemCallModel>(model)
{
    public override string ToString() =>
$$"""

public static readonly QueryDescription {{Model.SystemCallQueryName}} = new()
{
    All = {{GetTypeArray(Model.AllDescription)}},
    Any = {{GetTypeArray(Model.AnyDescription)}},
    None = {{GetTypeArray(Model.NoneDescription)}},
    Exclusive = {{GetTypeArray(Model.OnlyDescription)}}
};
""";

    private static string GetTypeArray(IEnumerable<string>? parameterSymbols)
    {
        if (parameterSymbols == null)
            return "[]";
        return $"[{string.Join(", ", parameterSymbols.Select(s => $"typeof({s})"))}]";
    }
}
