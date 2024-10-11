using DeltaGenCore;

namespace DeltaGen.Attributes;

internal class SystemCallAttribute : AttributeTemplate
{
    public override string Name => nameof(SystemCallAttribute);
    public override string ToString() =>
$$"""
#if {{Constants.GenerateAttributes}}


namespace Delta;

[System.AttributeUsage(System.AttributeTargets.Method)]
public class {{Name}} : System.Attribute { }

#endif
""";
}