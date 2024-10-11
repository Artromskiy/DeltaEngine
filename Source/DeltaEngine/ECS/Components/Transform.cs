using Delta.ECS.Attributes;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Delta.ECS.Components;

[Component(0, true), Dirty]
public struct Transform
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public Transform()
    {
        position = Vector3.Zero;
        rotation = Quaternion.Identity;
        scale = Vector3.One;
    }

    public readonly Matrix4x4 LocalMatrix
    {
        [Imp(Inl)]
        get => ModelMatrix(position, rotation, scale);
    }

    [Imp(Inl)]
    private readonly Matrix4x4 LocalMatrixSlow() => Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(position);

    [Imp(Inl)]
    private static Matrix4x4 ModelMatrix(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // every element in matrix except of translation
        // ends up being multiplied by 2, so we multiply whole vector by sqrt2
        // to skip multiplication at the end
        const float sqrt2 = 1.4142135623730951f;
        Vector4 rot = Unsafe.As<Quaternion, Vector4>(ref rotation) * sqrt2;

        // xx is actually equals to rotation.x * rotation.x * 2
        float xx = rot.X * rot.X;
        float yy = rot.Y * rot.Y;
        float zz = rot.Z * rot.Z;

        float xy = rot.X * rot.Y;
        float xz = rot.X * rot.Z;
        float xw = rot.X * rot.W;
        float yz = rot.Y * rot.Z;
        float yw = rot.Y * rot.W;
        float zw = rot.Z * rot.W;

        Matrix4x4 mat4 = default;
        mat4.M12 = scale.Y * (xy + zw);
        mat4.M13 = scale.Z * (xz - yw);
        mat4.M21 = scale.X * (xy - zw);
        mat4.M23 = scale.Z * (yz + xw);
        mat4.M31 = scale.X * (xz + yw);
        mat4.M32 = scale.Y * (yz - xw);
        mat4.M11 = scale.X * (1f - (yy + zz));
        mat4.M22 = scale.Y * (1f - (xx + zz));
        mat4.M33 = scale.Z * (1f - (xx + yy));
        mat4.M41 = translation.X;
        mat4.M42 = translation.Y;
        mat4.M43 = translation.Z;
        mat4.M44 = 1;

        return mat4;
    }
}