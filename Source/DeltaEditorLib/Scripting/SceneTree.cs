using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;
using System.Collections.Generic;
namespace DeltaEditorLib.Scripting;

public static class SceneTree
{
    public static Dictionary<EntityReference, List<EntityReference>> GeneratePreTree(EntityReference[] entities)
    {
        Dictionary<EntityReference, List<EntityReference>> preTree = [];
        foreach (var child in entities)
        {
            EntityReference parent = EntityReference.Null;
            if (child.Entity.TryGet<ChildOf>(out var childOf))
                parent = childOf.parent;
            if (!preTree.ContainsKey(child))
                preTree[child] = [];
            if (!preTree.TryGetValue(parent, out var list))
                preTree[parent] = list = [];
            list.Add(child);
        }
        return preTree;
    }
}
