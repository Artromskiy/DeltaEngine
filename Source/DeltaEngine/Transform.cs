using System.Numerics;

namespace DeltaEngine;
internal class Transform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public Matrix4x4 ModelMatrix =>  Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale);
}
