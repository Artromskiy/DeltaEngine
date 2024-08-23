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

    public GpuCameraData(Camera camera, Matrix4x4 worldMatrix)
    {
        Matrix4x4.Decompose(worldMatrix, out var _, out rotation, out var position3);
        var fwd = Vector3.Transform(Vector3.UnitZ, rotation);
        var up = Vector3.Transform(Vector3.UnitY, rotation);
        var view = Matrix4x4.CreateLookToLeftHanded(position3, fwd, up);
        position = new(position3, 0);
        proj = camera.projection;
        projView = Matrix4x4.Multiply(view, camera.projection); // inverted order, as vulkan/opengl uses other memory layout for matrices
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
