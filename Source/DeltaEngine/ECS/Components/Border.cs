using Delta.Assets;
using Delta.ECS.Attributes;
using System.Numerics;

namespace Delta.ECS.Components;
[Component]
public struct Border
{
    public Vector4 minMax;
    public Vector4 margin;
    public Vector4 padding;
    public Vector4 colors;
    public Vector4 cornerRadius;
    public Vector4 borderColors;
    public Vector4 borderThickness;
    public GuidAsset<ShaderData> shader;
}