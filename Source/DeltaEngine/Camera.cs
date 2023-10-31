using System.Numerics;

namespace DeltaEngine;
internal class Camera
{
    public Matrix4x4 projection; // Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, near, far);
    public Matrix4x4 view;
}
