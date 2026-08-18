using Delta.Engine.Assets;
using Delta.Engine.ECS.Attributes;
using Delta.Engine.Rendering;
using Delta.Maths;

using Delta.Engine.ECS.Components;
[Component]
public struct Border
{
    public float4 minMax;
    public float4 uv;
    public float4 margin;
    public float4 padding;
    //public Color colors;
    //public Color borderColors;
    public float4 cornerRadius;
    public int borderThickness;
    public GuidAsset<ShaderData> shader;

    public Border()
    {
        minMax = new(-1, -1, 1, 1);
    }
}
