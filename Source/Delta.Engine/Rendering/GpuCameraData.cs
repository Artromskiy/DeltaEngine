using Delta.Engine.ECS.Components;
using Delta.Maths;
using System.Runtime.InteropServices;

namespace Delta.Engine.Rendering;

[StructLayout(LayoutKind.Sequential)]
internal struct GpuCameraData
{
    public float4x4 projView;

    public float4x4 proj;
    public float4x4 view;

    public float4 position;
    public quaternion rotation;

    public GpuCameraData(Camera camera, float4x4 worldMatrix, float? aspect = null)
    {
        float4x4.Decompose(worldMatrix, out var _, out rotation, out var position3);
        var fwd = rotation * new float3(0, 0, -1);
        var up = rotation * new float3(0, 1, 0);
        position = new float4(position3, 0);
        view = float4x4.identity;
        proj = float4x4.identity;
        view = float4x4.CreateLookTo(position3, fwd, up);
        proj = GetProjection(camera, aspect);
        projView = proj * view;
    }

    private static float4x4 GetProjection(Camera camera, float? aspect = null)
    {
        float fovRadians = Maths.Radians(camera.fieldOfView);
        fovRadians = float.Clamp(fovRadians, float.Epsilon, float.Pi - float.Epsilon);
        float nearPlane = float.Max(camera.nearPlaneDistance, float.Epsilon);
        float farPlane = float.Max(camera.farPlaneDistance, nearPlane + float.Epsilon);
        return float4x4.CreatePerspectiveFieldOfViewLeftHanded(fovRadians, aspect ?? camera.aspectRation, nearPlane, farPlane);
    }

    public static GpuCameraData DefaultCamera() => new()
    {
        position = default,
        rotation = quaternion.identity,
        proj = float4x4.identity,
        view = float4x4.identity,
        projView = float4x4.identity
    };
}
