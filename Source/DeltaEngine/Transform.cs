using System.Numerics;

namespace DeltaEngine;
internal struct Transform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public readonly Matrix4x4 ModelMatrix => Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale);
}
