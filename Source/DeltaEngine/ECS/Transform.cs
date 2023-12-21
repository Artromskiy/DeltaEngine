using Delta.ECS;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Delta;

public struct Transform : IDirty
{
    internal Vector4 _position;
    public Quaternion Rotation;
    internal Vector4 _scale;

    /// <summary>
    /// Local Position
    /// </summary>
    public Vector3 Position
    {
        [MethodImpl(Inl)]
        readonly get => new(_position.X, _position.Y, _position.Z);
        [MethodImpl(Inl)]
        set => _position = new(value, 0);
    }

    /// <summary>
    /// Local Scale
    /// </summary>
    public Vector3 Scale
    {
        [MethodImpl(Inl)]
        readonly get => new(_scale.X, _scale.Y, _scale.Z);
        [MethodImpl(Inl)]
        set => _scale = new(value, 0);
    }

    public readonly Matrix4x4 LocalMatrix
    {
        [MethodImpl(Inl)]
        [SkipLocalsInit]
        //get => Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
        get => Matrix4x4.Transform(Matrix4x4.CreateScale(Scale), Rotation) * Matrix4x4.CreateTranslation(Position);
    }

}
