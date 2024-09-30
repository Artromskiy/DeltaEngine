using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;
using Delta.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Delta.ECS;

internal class HierarchySystem
{
    private readonly Dictionary<EntityReference, LinkedListNode<TreeNode>> _entityToNode = [];

    private readonly TreeNode Root = new();

    private readonly Stack<LinkedListNode<TreeNode>> _cachedNodes = [];
    private readonly Stack<TreeNode> _cachedTreeNodes = [];

    public ISystem MarkDestroySystem() => new MarkChildrenDestroy(this);

    public int GetOrderIndex(EntityReference entityRef)
    {
        Debug.Assert(entityRef.Entity.Has<Order>());
        Debug.Assert(entityRef.Entity.Has<HierarchyFlag>());

        var parentNode = GetParentNode(entityRef);
        var search = _entityToNode[entityRef];
        var node = parentNode.children.First!;
        var count = parentNode.children.Count;
        for (int i = 0; i < count; i++, node = node.Next!)
            if (node.Value.Equals(search.Value))
                return i;
        Debug.Assert(false);
        return -1;
    }

    public int RootEntitiesCount => Root.children.Count;
    public int EntitiesCount => _entityToNode.Count;

    public void UpdateOrders() => UpdateOrders(Root.children);

    public EntityReference[] GetRootEntities()
    {
        var children = Root.children;
        var count = children.Count;
        var node = children.First!;
        EntityReference[] references = new EntityReference[count];
        for (int i = 0; i < count; i++, node = node.Next!)
            references[i] = node.Value.entityRef;
        return references;
    }

    public void GetChildren(EntityReference entityRef, List<EntityReference> children)
    {
        Debug.Assert(entityRef.Entity.Has<Order>());
        Debug.Assert(entityRef.Entity.Has<HierarchyFlag>());

        var node = _entityToNode[entityRef];
        GetChildren(children, node.Value.children);
    }

    public List<EntityReference> GetFirstChildren(EntityReference entityRef)
    {
        Debug.Assert(entityRef.Entity.Has<Order>());
        Debug.Assert(entityRef.Entity.Has<HierarchyFlag>());

        List<EntityReference> children = [];
        GetFirstChildren(entityRef, children);
        return children;
    }

    public void GetFirstChildren(EntityReference entityRef, List<EntityReference> children)
    {
        Debug.Assert(entityRef.Entity.Has<Order>());
        Debug.Assert(entityRef.Entity.Has<HierarchyFlag>());

        var node = _entityToNode[entityRef];

        foreach (var item in node.Value.children)
            children.Add(item.entityRef);
    }

    public int GetFirstChildrenCount(EntityReference entityRef)
    {
        Debug.Assert(entityRef.Entity.Has<Order>());
        Debug.Assert(entityRef.Entity.Has<HierarchyFlag>());

        var node = _entityToNode[entityRef];
        return node.Value.children.Count;
    }

    public List<EntityReference> GetChildren(EntityReference entityRef)
    {
        Debug.Assert(entityRef.Entity.Has<Order>());
        Debug.Assert(entityRef.Entity.Has<HierarchyFlag>());

        List<EntityReference> children = [];
        GetChildren(entityRef, children);
        return children;
    }

    public EntityReference[] GetSiblings(EntityReference entityRef)
    {
        Debug.Assert(entityRef.Entity.Has<Order>());
        Debug.Assert(entityRef.Entity.Has<HierarchyFlag>());

        var parentNode = GetParentNode(entityRef);
        var node = parentNode.children.First!;
        var count = parentNode.children.Count;
        EntityReference[] siblings = new EntityReference[count];
        for (int i = 0; i < count; i++, node = node.Next!)
            siblings[i] = node.Value.entityRef;
        return siblings;
    }

    [Imp(Sync)]
    public void AddRootEntity(EntityReference entityRef)
    {
        Debug.Assert(!entityRef.Entity.Has<ChildOf>());
        Debug.Assert(!entityRef.Entity.Has<Order>());
        Debug.Assert(!entityRef.Entity.Has<HierarchyFlag>());

        int order = Root.children.Count;
        CreateMarkerAndOrder(entityRef, order);
        var node = GetOrCreateNode();
        _entityToNode[entityRef] = node;
        node.Value.entityRef = entityRef;
        Root.children.AddLast(node);
    }


    private static void UpdateOrders(LinkedList<TreeNode> children)
    {
        var node = children.First!;
        int count = children.Count;
        for (int i = 0; i < count; i++, node = node.Next!)
        {
            node.Value.entityRef.Entity.Get<Order>().order = i;
            UpdateOrders(node.Value.children);
        }
    }

    private static void GetChildren(List<EntityReference> entities, LinkedList<TreeNode> children)
    {
        foreach (var item in children)
        {
            entities.Add(item.entityRef);
            GetChildren(entities, item.children);
        }
    }

    private LinkedListNode<TreeNode> GetOrCreateNode()
    {
        if (!_cachedNodes.TryPop(out var node))
            node = new LinkedListNode<TreeNode>(default);
        if (!_cachedTreeNodes.TryPop(out node.ValueRef))
            node.Value = new TreeNode();
        return node;
    }

    private void RemoveNode(TreeNode treeNode)
    {
        var node = treeNode.children.First!;
        int count = treeNode.children.Count;
        for (int i = 0; i < count; i++, node = node.Next!)
            _cachedNodes.Push(node);
        treeNode.children.Clear();
        treeNode.entityRef = EntityReference.Null;
        _cachedTreeNodes.Push(treeNode);
    }

    private TreeNode GetParentNode(EntityReference entityRef)
    {
        var entity = entityRef.Entity;
        Debug.Assert(entity.Has<HierarchyFlag>());

        TreeNode parent = Root;
        if (entity.TryGet<ChildOf>(out var childOf))
        {
            Debug.Assert(childOf.parent.Entity.Has<HierarchyFlag>());
            parent = _entityToNode[childOf.parent].Value;
        }
        return parent;
    }

    private static void CreateMarkerAndOrder(EntityReference entityRef, int order)
    {
        Debug.Assert(!entityRef.Entity.Has<Order>());
        Debug.Assert(!entityRef.Entity.Has<HierarchyFlag>());

        var markerCmp = new HierarchyFlag();
        var orderCmp = new Order(order);
        entityRef.Entity.Add(markerCmp, orderCmp);
    }


    private struct HierarchyFlag { }

    private class TreeNode : IEquatable<TreeNode>
    {
        public EntityReference entityRef;
        public readonly LinkedList<TreeNode> children;

        public TreeNode(EntityReference entityRef)
        {
            children = [];
            this.entityRef = entityRef;
        }
        public TreeNode() : this(EntityReference.Null) { }

        public override bool Equals(object? obj) => obj is TreeNode node && Equals(node);
        public bool Equals(TreeNode? other) => other is not null && other.entityRef == entityRef;
        public override int GetHashCode() => entityRef.GetHashCode();

        public static bool operator ==(TreeNode left, TreeNode right) => left.Equals(right);
        public static bool operator !=(TreeNode left, TreeNode right) => !left.Equals(right);
    }


    private readonly struct MarkChildrenDestroy(HierarchySystem hierarchySystem) : ISystem
    {
        private static readonly QueryDescription _destroyDescription = new QueryDescription().WithAll<DestroyFlag, HierarchyFlag, Order>();
        private static readonly List<Entity> _entitiesToDestroy = [];
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;

            _entitiesToDestroy.Clear();
            world.GetEntities(_destroyDescription, _entitiesToDestroy);
            if (_entitiesToDestroy.Count == 0)
                return;

            // Do not include items which has parent with DestroyFlag
            _entitiesToDestroy.RemoveAll(static x => x.GetLastParent<DestroyFlag>(out var _));

            foreach (var entity in _entitiesToDestroy)
            {
                var entityRef = world.Reference(entity);
                var parentNode = hierarchySystem.GetParentNode(entityRef);
                var node = hierarchySystem._entityToNode[entityRef];

                RemoveEntities(node.Value);
                parentNode.children.Remove(node);
                hierarchySystem._cachedNodes.Push(node);
            }
        }

        private readonly void RemoveEntities(TreeNode treeNode)
        {
            foreach (var item in treeNode.children)
                RemoveEntities(item);
            treeNode.entityRef.Entity.AddOrGet<DestroyFlag>();
            hierarchySystem.RemoveNode(treeNode);
        }
    }
}