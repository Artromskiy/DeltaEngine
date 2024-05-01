using Delta.Scripting;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Delta.ECS.Components;

[Component(0, true)]
public struct Transform : IDirty
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public readonly Matrix4x4 LocalMatrix
    {
        [MethodImpl(Inl)]
        get => ModelMatrix(position, rotation, scale);
    }


    [MethodImpl(Inl)]
    private readonly Matrix4x4 LocalMatrixSlow() => Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale) * Matrix4x4.CreateTranslation(position);

    [MethodImpl(Inl)]
    public static Matrix4x4 ModelMatrix(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        // Faster simd creation of xx, xy, xz, xw
        var x = rotation.X * Unsafe.As<Quaternion, Vector4>(ref rotation);

        float yy = rotation.Y * rotation.Y;
        float yz = rotation.Y * rotation.Z;
        float yw = rotation.Y * rotation.W;

        float zz = rotation.Z * rotation.Z;
        float zw = rotation.Z * rotation.W;

        float scaleX2 = scale.X * 2;
        float scaleY2 = scale.Y * 2;
        float scaleZ2 = scale.Z * 2;
        Matrix4x4 modelMatrix = Matrix4x4.Identity;
        modelMatrix.M11 = scale.X - (scaleX2 * (yy + zz));
        modelMatrix.M12 = scaleY2 * (x.Y - zw);
        modelMatrix.M13 = scaleZ2 * (x.Z + yw);
        modelMatrix.M21 = scaleX2 * (x.Y + zw);
        modelMatrix.M22 = scale.Y - (scaleY2 * (x.X + zz));
        modelMatrix.M23 = scaleZ2 * (yz - x.W);
        modelMatrix.M31 = scaleX2 * (x.Z - yw);
        modelMatrix.M32 = scaleY2 * (yz + x.W);
        modelMatrix.M33 = scale.Z - (scaleZ2 * (x.X + yy));
        modelMatrix.M41 = translation.X;
        modelMatrix.M42 = translation.Y;
        modelMatrix.M43 = translation.Z;

        return modelMatrix;
    }
}