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
        var fwd = Vector3.Transform(Vector3.UnitZ, rotation);
        var up = Vector3.Transform(Vector3.UnitY, rotation);
        var view = Matrix4x4.CreateLookToLeftHanded(position3, fwd, up);
        position = new(position3, 0);
        proj = GetProjection(camera, aspect);
        projView = Matrix4x4.Multiply(view, proj); // inverted order, as vulkan/opengl uses other memory layout for matrices
    }

    private static Matrix4x4 GetProjection(Camera camera, float? aspect = null)
    {
        float fovRadians = float.DegreesToRadians(camera.fieldOfView);
        return Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(fovRadians, aspect ?? camera.aspectRation, camera.nearPlaneDistance, camera.farPlaneDistance);
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
