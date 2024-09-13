using Arch.Core;
using Delta.Runtime;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DeltaEditor.Hierarchy
{
    public class HierarchyNodeCreator
    {
        private readonly Stack<HierarchyNodeControl> _nodes = [];
        private readonly Dictionary<EntityReference, bool> _collapsedData = [];

        private readonly List<EntityReference> _childrenListCached = [];

        public event Action<EntityReference>? OnEntitySelectRequest;
        public event Action<EntityReference>? OnEntityRemoveRequest;

        public void CallRemove(EntityReference entityRef)=> OnEntityRemoveRequest?.Invoke(entityRef);
        public void CallSelect(EntityReference entityRef)=> OnEntitySelectRequest?.Invoke(entityRef);


        public bool IsCollapsed(EntityReference entityRef)
        {
            if (!_collapsedData.TryGetValue(entityRef, out var collapsed))
                return true;
            return collapsed;
        }

        public bool SetCollapsed(EntityReference entityRef, bool collapsed) => _collapsedData[entityRef] = collapsed;

        public ReadOnlySpan<EntityReference> GetChildren(IRuntimeContext context, EntityReference entityRef)
        {
            context.SceneManager.CurrentScene.GetFirstChildren(entityRef, _childrenListCached);
            return CollectionsMarshal.AsSpan(_childrenListCached);
        }

        public HierarchyNodeControl GetOrCreateNode()
        {
            HierarchyNodeControl node;
            if (!_nodes.TryPop(out node!))
                node = new HierarchyNodeControl(this);
            return node;
        }

        public void ReturnNode(HierarchyNodeControl node)
        {
            _nodes.Push(node);
        }
    }
}
