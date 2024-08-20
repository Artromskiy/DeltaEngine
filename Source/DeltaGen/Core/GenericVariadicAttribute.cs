﻿using DeltaGen.Core;
using System.Linq;

namespace DeltaGen.Templates;

internal class GenericVariadicAttribute : AttributeTemplate
{
    public override string ToString() =>
$$"""
#if {{Constants.GenerateAttributes}}

using System;
namespace Delta;

{{Loop(Enumerable.Range(1, Constants.VariadicCount).Select(GenericAttribute))}}

#endif
""";

    private string GenericAttribute(int count) =>$$"""public class {{Name}}<{{GenericArguments(count)}}> : Attribute { }""";
    private static string GenericArguments(int count) => string.Join(", ", Enumerable.Range(0, count).Select(t => $"T{t}"));
}