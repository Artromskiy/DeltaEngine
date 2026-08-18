using DVG.Engine.Generation.Core;
using System.Linq;

namespace DVG.Engine.Generation.Attributes;

internal class GenericVariadicAttribute : AttributeTemplate
{
    public override string ToString() =>
$$"""
#if {{Constants.GenerateAttributes}}

using System;
using DVG.Engine;

{{LoopSelect(Enumerable.Range(1, Constants.VariadicCount), GenericAttribute)}}

#endif
""";

    private string GenericAttribute(int count) => $$"""public class {{Name}}<{{GenericArguments(count)}}> : Attribute { }""";
    private static string GenericArguments(int count) => string.Join(", ", Enumerable.Range(0, count).Select(t => $"T{t}"));
}
