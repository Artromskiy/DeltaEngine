using DVG.Engine.Generation.Core;

namespace DVG.Engine.Generation.Attributes;

internal class SystemCallAttribute : AttributeTemplate
{
    public override string Name => nameof(SystemCallAttribute);
    public override string ToString() =>
$$"""
#if {{Constants.GenerateAttributes}}


namespace DVG.Engine;

[System.AttributeUsage(System.AttributeTargets.Method)]
public class {{Name}} : System.Attribute { }

#endif
""";
}
