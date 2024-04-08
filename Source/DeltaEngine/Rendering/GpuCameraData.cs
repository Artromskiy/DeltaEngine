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
}
