using Arch.Core;
using DeltaEngine.ECS;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DeltaEngine;

public struct Transform : IDirty<Transform>
{
    private Vector3 _position;
    private Quaternion _rotation;
    private Vector3 _scale;

    /// <summary>
    /// Local Position
    /// </summary>
    public Vector3 Position
    {
        [MethodImpl(Inl)]
        readonly get => _position;
        [MethodImpl(Inl)]
        set => this.Set(ref _position, ref value);
    }
    /// <summary>
    /// Local Rotation
    /// </summary>
    public Quaternion Rotation
    {
        [MethodImpl(Inl)]
        readonly get => _rotation;
        [MethodImpl(Inl)]
        set => this.Set(ref _rotation, ref value);
    }
    /// <summary>
    /// Local Scale
    /// </summary>
    public Vector3 Scale
    {
        [MethodImpl(Inl)]
        readonly get => _scale;
        [MethodImpl(Inl)]
        set => this.Set(ref _scale, ref value);
    }

    public readonly Matrix4x4 LocalMatrix
    {
        [MethodImpl(Inl)]
        [SkipLocalsInit]
        //get => Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
        get => Matrix4x4.Transform(Matrix4x4.CreateScale(Scale), Rotation) * Matrix4x4.CreateTranslation(Position);
    }

    bool IDirty.IsDirty { get; set; }
}

internal struct ChildOf
{
    public EntityReference parent;
}