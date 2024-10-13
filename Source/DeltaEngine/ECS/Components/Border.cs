using Delta.Assets;
using Delta.ECS.Attributes;
using Delta.Rendering;
using System.Numerics;

namespace Delta.ECS.Components;
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