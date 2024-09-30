using Delta.ECS.Attributes;
using System.Collections.Generic;
using System.Numerics;

namespace Delta.UI;
[Component]
public struct Border
{
    public Vector4 MinMax;
    public Vector4 margin;
    public Vector4 padding;
    public Vector4 colors;
    public Vector4 cornerRadius;
    public Vector4 borderColors;
    public Vector4 borderThickness;
    public List<Border> Children;

}