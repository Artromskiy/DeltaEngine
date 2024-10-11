using DeltaGenCore;

namespace GLCSharp.Attributes
{
    internal class ShaderAttribute : AttributeTemplate
    {
        public override string Name => nameof(ShaderAttribute);

        public override string ToString() =>
$$"""
#if {{Constants.GenerateAttributes}}

using System;

namespace Delta;

[System.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class {{Name}} : System.Attribute { }

#endif
""";

    }
}
