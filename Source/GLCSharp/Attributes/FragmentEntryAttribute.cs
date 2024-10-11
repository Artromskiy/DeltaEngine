using DeltaGenCore;

namespace GLCSharp.Attributes;

internal class FragmentEntryAttribute : AttributeTemplate
{
    public override string Name => nameof(FragmentEntryAttribute);

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