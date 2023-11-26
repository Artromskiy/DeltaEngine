namespace DeltaEngine.Rendering;
internal readonly struct Material
{
    public readonly ShaderData shader;
    public Material(ShaderData shader)
    {
        this.shader = shader;
    }
}
