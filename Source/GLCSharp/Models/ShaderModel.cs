using DeltaGenCore;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace GLCSharp.Models
{
    internal record ShaderModel : Model
    {
        public INamedTypeSymbol TypeSymbol { get; }

        public string TypeName => TypeSymbol.Name;
        public string TypeFileName => $"{TypeSymbol.Name}File";
        public string TypeNamespace => TypeSymbol.ContainingNamespace.ToDisplayString();
        public override string Name
        {
            get => TypeSymbol.Name;
            set => _ = value;
        }

        public ShaderModel(INamedTypeSymbol typeSymbol)
        {
            TypeSymbol = typeSymbol;
        }

        public IEnumerable<INamedTypeSymbol> ContainingTypes()
        {
            List<INamedTypeSymbol> symbols = [];
            var current = TypeSymbol.ContainingType;
            while (current != null)
            {
                symbols.Add(current);
                current = current.ContainingType;
            }
            symbols.Reverse();
            return symbols;
        }
        public int ContainingTypesCount()
        {
            int count = 0;
            var current = TypeSymbol.ContainingType;
            while (current != null)
            {
                count++;
                current = current.ContainingType;
            }
            return count;
        }

    }
}
