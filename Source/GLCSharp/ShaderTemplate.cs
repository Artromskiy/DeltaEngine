using DeltaGenCore;
using GLCSharp.Models;
using System.Linq;

namespace GLCSharp;

internal class ShaderTemplate(ShaderModel model) : Template<ShaderModel>(model)
{
    public override string Name => string.Join(".", Model.ContainingTypes().Select(s => s.Name).Concat([Model.Name]));
    public override string ToString() =>
$$"""
using System;

namespace {{Model.TypeNamespace}};

{{LoopSelect(Model.ContainingTypes(), s => $"{s.TypeDeclaration()} {{ \n")}}

    {{Model.TypeSymbol.TypeDeclaration()}}
    {
        public readonly Vector3 Pos3;
        public readonly Vector2 Pos2;
        public readonly Vector4 Color;
        public readonly Vector2 Tex;
        public readonly Vector3 Norm;
        public readonly Vector3 Tan;
        public readonly Vector3 Binorm;
        public readonly Vector3 Bitan;
    }

{{LoopRange(Model.ContainingTypesCount(), s => "}\n")}}

""";
}
