using Arch.Core;
using Arch.Core.Extensions;
using System.Numerics;

namespace DeltaEngine.ECS;

internal class TransformSystem : ComponentRegistry<Transform>
{
    public TransformSystem(World world) : base(world)
    {

    }

    public static Transform GetWorld(Entity entity)
    {
        ref var transform = ref entity.TryGetRef<Transform>(out bool hasTransform);
        if (!hasTransform)
            return default;
        bool hasParent = entity.GetParent(out var parent);
        if (!hasParent)
            return transform;

        var local = transform.LocalMatrix;
        while (hasParent)
        {
            transform = ref parent.TryGetRef<Transform>(out hasTransform);
            if (hasTransform)
                local *= transform.LocalMatrix;
            hasParent = parent.GetParent(out parent);
        }
        Transform result = new();
        var decomposed = Matrix4x4.Decompose(local, out var scale, out var rotation, out var position);
        result.Scale = scale;
        result.Rotation = rotation;
        result.Position = position;
        return result;
    }
}