using DeltaGen.Core;

namespace DeltaGen.Attributes;

internal class SystemCallAttribute : AttributeTemplate
{
    public override string Name => nameof(SystemCallAttribute);
    public override string ToString() =>
$$"""

namespace Delta;

[System.AttributeUsage(System.AttributeTargets.Method)]
public class {{Name}} : System.Attribute { }

""";
}