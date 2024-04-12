using Delta.Scripting;
using System.Numerics;

namespace Delta.ECS.Components;

[Component]
public struct Camera
{
    public Matrix4x4 projection;
}