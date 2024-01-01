using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Delta.ECS;

[StructLayout(LayoutKind.Sequential)]
public struct Transform : IDirty
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;



    public Matrix4x4 LocalMatrix
    {
        [MethodImpl(Inl)]
        //get => Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateTranslation(Position);
        //get => Matrix4x4.Transform(Matrix4x4.CreateScale(Scale), Rotation) * Matrix4x4.CreateTranslation(Position);
        get => LocalOptimized();
        //get => Matrix4x4.Identity;
    }

    [MethodImpl(Inl)]
    public Matrix4x4 LocalOptimized()
    {
        ref readonly var t = ref Position;
        ref readonly var r = ref Rotation;
        ref readonly var s = ref Scale;
        var rYrY = r.Y * r.Y;
        var rZrZ = r.Z * r.Z;
        var rZrW = r.Z * r.W;
        var rYrW = r.Y * r.W;
        var rXrX = r.X * r.X;
        var rXrW = r.X * r.W;
        var rXrZ = r.X * r.Z;
        var rXrY = r.X * r.Y;
        var rYrZ = r.Y * r.Z;
        var sX2 = s.X * 2;
        var sY2 = s.Y * 2;
        var sZ2 = s.Z * 2;
        Matrix4x4 res = new
        (
            (1.0f - (2.0f * (rYrY + rZrZ))) * s.X,
            (rXrY - rZrW) * sY2,
            (rXrZ + rYrW) * sZ2,
            t.X,
            (rXrY + rZrW) * sX2,
            (1.0f - (2.0f * (rXrX + rZrZ))) * s.Y,
            (rYrZ - rXrW) * sZ2,
            t.Y,
            (rXrZ - rYrW) * sX2,
            (rYrZ + rXrW) * sY2,
            (1.0f - (2.0f * (rXrX + rYrY))) * s.Z,
            t.Z,
            0.0f,
            0.0f,
            0.0f,
            1.0f
        );
        return res;
    }
}
