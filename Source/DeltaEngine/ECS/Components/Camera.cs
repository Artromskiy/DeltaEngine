using System.Numerics;

namespace Delta.ECS.Components;
internal struct Camera
{
    public Matrix4x4 projection;
    public Matrix4x4 view;
}