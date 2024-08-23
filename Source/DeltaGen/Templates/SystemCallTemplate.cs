using DeltaGen.Core;
using DeltaGen.Models;

namespace DeltaGen.Templates;

internal class SystemCallTemplate(SystemCallModel model) : Template<SystemCallModel>(model)
{
    private const string WorldParameter = "world";
    private const string SystemParameter = "system";
    public override string ToString() =>
$$"""

{{SystemCallMethodDeclaration()}}
{
    var query = {{WorldParameter}}.Query(in {{Model.System.TypeFileName}}.{{Model.SystemCallQueryName}});
    foreach (ref var chunk in query)
    {
        {{ChunkGetFirst()}}
        foreach (var entityIndex in chunk)
        {
            {{AddRefIterate()}}
            {{MethodCall()}}
        }
    }
}
""";

    private string ChunkGetFirst()
    {
        return Model.Parameters.Length switch
        {
            0 => string.Empty,
            1 => $"ref var componentsFirst = ref chunk.GetFirst<{string.Join(", ", Model.ParametersTypes)}>();",
            _ => $"var componentsFirst = chunk.GetFirst<{string.Join(", ", Model.ParametersTypes)}>();",
        };
    }

    private string SystemCallMethodDeclaration()
    {
        bool isMethodStatic = Model.MethodSymbol.IsStatic;
        string methodParameters = isMethodStatic ?
            $"World {WorldParameter}" :
            $"{Model.System.ParameterModifierString} {Model.System.TypeName} {SystemParameter}, World {WorldParameter}";
        return $"private static void {Model.SystemCallMethodName}({methodParameters})";
    }

    private string AddRefIterate()
    {
        return Model.Parameters.Length switch
        {
            0 => string.Empty,
            1 => "ref var tComponent = ref Unsafe.Add(ref componentsFirst, entityIndex);",
            _ => Loop("ref var t{0}Component = ref Unsafe.Add(ref componentsFirst.t{0}, entityIndex);", Model.ParametersIndices),
        };
    }

    private string MethodCall()
    {
        return Model.Parameters.Length switch
        {
            0 => $"{Instance}{Model.MethodName}();",
            1 => $"{Instance}{Model.MethodName}({Join("{0} tComponent", ", ", Model.ArgumentModifiers)});",
            _ => $"{Instance}{Model.MethodName}({Join("{0} t{1}Component", ", ", Model.ArgumentModifiers, Model.ParametersIndices)});",
        };
    }
    private string Instance => Model.MethodSymbol.IsStatic ? string.Empty : $"{SystemParameter}.";
}