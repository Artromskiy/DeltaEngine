using DeltaEngine.Rendering;
using System.Collections.Generic;
using System.Numerics;

namespace DeltaEngine.ECS;
internal class TransformSystem
{
    private readonly StackList<Transform> _transforms = new();
    private readonly StackList<HashSet<int>> _childs = new();

    public void Add(ref Transform transform)
    {
        transform.parent = -1;
        transform.id = _transforms.Next();
        _transforms.Add(transform);
        _childs.Add(null!);
    }

    public Matrix4x4 GetWorld(ref Transform transform)
    {
        int parentId = transform.parent;
        var local = transform.LocalMatrix;
        while (parentId != -1)
        {
            var parent = _transforms[parentId];
            local *= parent.LocalMatrix;
            parentId = parent.id;
        }
        return local;
    }
}
