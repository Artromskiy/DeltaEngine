using Delta.ECS.Attributes;

namespace Delta.ECS.Components;

[Component(0)]
public struct Camera
{
    public float fieldOfView;
    public float aspectRation;
    public float nearPlaneDistance;
    public float farPlaneDistance;

    public Camera()
    {
        fieldOfView = 90;
        aspectRation = 1;
        nearPlaneDistance = 0;
        farPlaneDistance = 1000;
    }
}