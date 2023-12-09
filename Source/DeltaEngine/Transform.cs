using Arch.Core;
using System.Numerics;

namespace DeltaEngine;

public struct Transform
{
    /// <summary>
    /// Local position
    /// </summary>
    public Vector3 position;
    /// <summary>
    /// Local rotation
    /// </summary>
    public Quaternion rotation;
    /// <summary>
    /// Local scale
    /// </summary>
    public Vector3 scale;
    
    public bool isStatic;
    public readonly Matrix4x4 LocalMatrix => Matrix4x4.CreateTranslation(position) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateScale(scale);
}

internal struct ChildOf
{
    public EntityReference parent;
}