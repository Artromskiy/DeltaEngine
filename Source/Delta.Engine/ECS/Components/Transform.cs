using Delta.Engine.ECS.Attributes;
using Delta.Maths;

namespace Delta.Engine.ECS.Components;

[Component(0, true), Dirty]
public struct Transform
{
    public float3 position;
    public quaternion rotation;
    public float3 scale;

    public Transform()
    {
        position = float3.zero;
        rotation = quaternion.identity;
        scale = new float3(1);
    }

    public readonly float4x4 LocalMatrix
    {
        [Imp(Inl)]
        get => float4x4.CreateTRS(position, rotation, scale);
    }
}
