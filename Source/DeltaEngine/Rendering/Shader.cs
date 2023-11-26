using System;

namespace DeltaEngine.Rendering;
public readonly struct Shader
{
    public readonly Guid guid;
    public Shader(Guid guid)
    {
        this.guid = guid;
    }
}