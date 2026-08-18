using DVG.Engine.Generation.Core;

namespace DVG.Engine.Generation.Attributes;

internal class SystemAttribute : AttributeTemplate
{
    public override string Name => nameof(SystemAttribute);

    public override string ToString() =>
$$"""
#if {{Constants.GenerateAttributes}}

using System;

namespace DVG.Engine;

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class {{Name}} : System.Attribute { }

#endif
""";
}
