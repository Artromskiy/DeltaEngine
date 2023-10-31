namespace DeltaEngine.Rendering;
internal readonly struct Material
{
    public readonly Shader shader;
    public Material(Shader shader)
    {
        this.shader = shader;
    }
}
