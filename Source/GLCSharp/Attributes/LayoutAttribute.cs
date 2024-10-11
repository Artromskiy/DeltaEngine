using DeltaGenCore;

namespace GLCSharp.Attributes;

internal class LayoutAttribute : AttributeTemplate
{
    public override string Name => nameof(LayoutAttribute);

    public override string ToString() =>
$$"""
#if {{Constants.GenerateAttributes}}

using System;

namespace Delta;

[System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class {{Name}} : System.Attribute
{
    public readonly int set = -1;
    public readonly int binding = -1;
    public readonly int location = -1;
    public {{Name}} (int set = -1, int binding = -1, int location = -1)
    {
        this.set = int.Max(set, this.set);
        this.binding = int.Max(binding, this.binding);
        this.location = int.Max(location, this.location);
    }
}

#endif
""";

}
