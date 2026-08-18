using DVG.Engine.Assets;
using DVG.Engine.ECS.Attributes;
using DVG.Engine.Rendering;
using System.Numerics;

using DVG.Engine.ECS.Components;
[Component]
public struct Border
{
    public Vector4 minMax;
    public Vector4 uv;
    public Vector4 margin;
    public Vector4 padding;
    //public Color colors;
    //public Color borderColors;
    public Vector4 cornerRadius;
    public int borderThickness;
    public GuidAsset<ShaderData> shader;

    public Border()
    {
        minMax = new(-1, -1, 1, 1);
    }
}
