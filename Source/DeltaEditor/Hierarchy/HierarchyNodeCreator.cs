using Arch.Core;
using Delta.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DeltaEditor.Hierarchy
{
    public class HierarchyNodeCreator
    {
        private readonly Stack<HierarchyNodeControl> _nodes = [];
        private readonly HashSet<EntityReference> _expandedNodes = [];

        private readonly List<EntityReference> _childrenListCached = [];

        public event Action<EntityReference>? OnEntitySelectRequest;
        public event Action<EntityReference>? OnEntityRemoveRequest;

        public void CallRemove(EntityReference entityRef) => OnEntityRemoveRequest?.Invoke(entityRef);
        public void CallSelect(EntityReference entityRef) => OnEntitySelectRequest?.Invoke(entityRef);


        public bool IsCollapsed(EntityReference entityRef)
        {
            return !_expandedNodes.Contains(entityRef);
        }

        public void SetCollapsed(EntityReference entityRef, bool collapsed)
        {
            if (collapsed)
                _expandedNodes.Remove(entityRef);
            else
                _expandedNodes.Add(entityRef);
        }

        public ReadOnlySpan<EntityReference> GetChildren(EntityReference entityRef)
        {
            IRuntimeContext.Current.SceneManager.CurrentScene.GetFirstChildren(entityRef, _childrenListCached);
            return CollectionsMarshal.AsSpan(_childrenListCached);
        }

        public int GetChildrenCount(EntityReference entityRef)
        {
            return IRuntimeContext.Current.SceneManager.CurrentScene.GetFirstChildrenCount(entityRef);
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
