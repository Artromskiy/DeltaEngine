using Delta.Scripting;

namespace Delta.ECS.Components;

[Component]
public struct Camera
{
    public float fieldOfView;
    public float aspectRation;
    public float nearPlaneDistance;
    public float farPlaneDistance;
}