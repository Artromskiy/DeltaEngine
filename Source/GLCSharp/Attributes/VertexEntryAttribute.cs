using DeltaGenCore;

namespace GLCSharp.Attributes;

internal class VertexEntryAttribute : AttributeTemplate
{
    public override string Name => nameof(VertexEntryAttribute);

    public override string ToString() =>
$$"""
#if {{Constants.GenerateAttributes}}

using System;

namespace Delta;

[System.AttributeUsage(AttributeTargets.Method)]
public class {{Name}} : System.Attribute { }

#endif
""";

}
