using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DeltaEngine.ECS;

namespace DeltaEngine.Rendering;

internal class MeshGroups
{
    private readonly Dictionary<Guid, StackList<Transform>> _transforms;

    private readonly HashSet<int> _addedEntities = new();

    public MeshGroups()
    {
        _transforms = new();
    }

    public void Add(ref RenderData data)
    {
        Debug.Assert(_addedEntities.Add(data.id));
        if (!_transforms.TryGetValue(data.mesh.guid, out var group))
            _transforms[data.mesh.guid] = group = new();
        data.renderGroupId = group.Add(data.transform);
        data.bindedGroup = group;
    }

    public void Remove(ref RenderData data)
    {
        Debug.Assert(_addedEntities.Remove(data.id));
        _transforms[data.mesh.guid].RemoveAt(data.renderGroupId);
        data.renderGroupId = -1;
        data.bindedGroup = default!;
    }
}
