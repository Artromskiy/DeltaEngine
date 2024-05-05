using DeltaGen.Core;
using System;

namespace DeltaGen.Attributes;

internal class SystemAttribute : AttributeTemplate
{
    public override string Name => nameof(SystemAttribute);

    public override string ToString() =>
$$"""

using System;

namespace Delta;

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class {{Name}} : System.Attribute { }

""";
}