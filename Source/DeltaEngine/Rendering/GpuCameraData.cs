using Delta.ECS.Components;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Delta.Rendering;

[StructLayout(LayoutKind.Sequential)]
internal struct GpuCameraData
{
    public Matrix4x4 projView;

    public Matrix4x4 proj;
    public Matrix4x4 view;

    public Vector4 position;
    public Quaternion rotation;

    public GpuCameraData(Camera camera, Matrix4x4 worldMatrix, float? aspect = null)
    {
        Matrix4x4.Decompose(worldMatrix, out var _, out rotation, out var position3);
        var fwd = Vector3.Transform(-Vector3.UnitZ, rotation);
        var up = Vector3.Transform(Vector3.UnitY, rotation);
        position = new(position3, 0);
        view = Matrix4x4.Identity;
        proj = Matrix4x4.Identity;
        view = Matrix4x4.CreateLookTo(position3, fwd, up);
        proj = GetProjection(camera, aspect);
        projView = Matrix4x4.Multiply(proj, view);
    }

    private static Matrix4x4 GetProjection(Camera camera, float? aspect = null)
    {
        float fovRadians = float.DegreesToRadians(camera.fieldOfView);
        fovRadians = float.Clamp(fovRadians, float.Epsilon, float.Pi - float.Epsilon);
        float nearPlane = float.Max(camera.nearPlaneDistance, float.Epsilon);
        float farPlane = float.Max(camera.farPlaneDistance, nearPlane + float.Epsilon);
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(fovRadians, aspect ?? camera.aspectRation, nearPlane, farPlane);
    }

    public static GpuCameraData DefaultCamera() => new()
    {
        position = default,
        rotation = Quaternion.Identity,
        proj = Matrix4x4.Identity,
        view = Matrix4x4.Identity,
        projView = Matrix4x4.Identity
    };
}