using Arch.Core;
using Arch.Core.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System;

namespace Delta.ECS.Components.Hierarchy;

internal class HierarchySystem
{
    private readonly Dictionary<EntityReference, LinkedListNode<TreeNode>> _entityToNode = [];

    private readonly TreeNode Root = new();

    private readonly Stack<LinkedListNode<TreeNode>> _cachedNodes = [];
    private readonly Stack<TreeNode> _cachedTreeNodes = [];


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

    public void UpdateOrders()
    {
        Stopwatch sw = Stopwatch.StartNew();
        UpdateOrders(Root.children);
        sw.Stop();
        Debug.WriteLine($"Update orders took {(int)sw.Elapsed.TotalMicroseconds}us");
    }

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
        node.ValueRef.entityRef = entityRef;
        Root.children.AddLast(node);
    }


    [Imp(Sync)]
    public void RemoveEntity(EntityReference entityRef)
    {
        Debug.Assert(entityRef.Entity.Has<HierarchyFlag>());
        Debug.Assert(entityRef.Entity.Has<Order>());

        var node = _entityToNode[entityRef];
        var parentNode = GetParentNode(entityRef);

        parentNode.children.Remove(node);

        RemoveEntities(node.Value.children);
    }

    private static void UpdateOrders(LinkedList<TreeNode> children)
    {
        var node = children.First!;
        int count = children.Count;
        for (int i = 0; i < count; i++, node = node.Next!)
        {
            node.ValueRef.entityRef.Entity.Get<Order>().order = i;
            UpdateOrders(node.ValueRef.children);
        }
    }

    private void RemoveEntities(LinkedList<TreeNode> children)
    {
        foreach (var item in children)
        {
            RemoveEntities(item.children);
            RemoveNode(item);
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
            node.ValueRef = new TreeNode();
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

    private struct TreeNode : IEquatable<TreeNode>
    {
        public EntityReference entityRef;
        public readonly LinkedList<TreeNode> children;

        public TreeNode(EntityReference entityRef)
        {
            children = [];
            this.entityRef = entityRef;
        }
        public TreeNode() : this(EntityReference.Null) { }


        public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is TreeNode node && Equals(node);
        public readonly bool Equals(TreeNode other) => other.entityRef == entityRef;
        public override readonly int GetHashCode() => entityRef.GetHashCode();

        public static bool operator ==(TreeNode left, TreeNode right) => left.Equals(right);
        public static bool operator !=(TreeNode left, TreeNode right) => !left.Equals(right);
    }
}