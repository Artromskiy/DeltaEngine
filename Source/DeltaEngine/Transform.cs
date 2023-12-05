using System.Numerics;

namespace DeltaEngine;

public struct Transform
{
    public Quaternion rotation;
    public Vector3 position;
    public Vector3 scale;

    internal int id;
    internal int parent;

    public readonly Matrix4x4 LocalMatrix => Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale);
}
