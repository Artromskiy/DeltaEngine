using Delta.ECS.Components;
using System.Numerics;

namespace DeltaEditor.EditorCamera;

internal class SceneCamera
{
    public Camera Camera = new()
    {
        projection = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(float.DegreesToRadians(90), 1, float.Epsilon, 1000)
    };
    public Transform Transform = new()
    {
        position = new Vector3(0, 0, -2),
        rotation = Quaternion.Identity,
        scale = Vector3.One
    };

    public void Move(Vector3 move)
    {
        Transform.position += move;
    }

    public void Rotate(Quaternion rotate)
    {
        Transform.rotation *= rotate;
    }
}