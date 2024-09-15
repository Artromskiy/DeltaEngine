using Arch.Core;
using Arch.Core.Extensions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Delta.ECS.Components;
using Delta.Runtime;
using DeltaEditor.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeltaEditor;

public partial class HierarchyControl : UserControl
{
    public event Action<EntityReference>? OnEntitySelected;
    private readonly HierarchyNodeCreator _hierarchyNodeCreator = new();
    private readonly IComparer<EntityReference> _entityReferenceComparer = new EntityReferenceComparer();

    private IListWrapper<HierarchyNodeControl, Control> ChildrenNodes => new(EntityNodeStack.Children);

    public HierarchyControl()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
            return;
        _hierarchyNodeCreator.OnEntityRemoveRequest += RemoveEntity;
        _hierarchyNodeCreator.OnEntitySelectRequest += SelectEntity;
    }

    public void UpdateHierarchy(IRuntimeContext ctx)
    {
        DebugTimer.StartDebug();

        if (ctx.SceneManager.CurrentScene == null)
            return;

        var entities = ctx.SceneManager.CurrentScene.GetRootEntities();
        int count = entities.Length;

        UpdateChildrenCount(count);

        for (int i = 0; i < count; i++)
            ChildrenNodes[i].UpdateEntity(ctx, entities[i]);

        DebugTimer.StopDebug();
    }


    private void CreateNewEntity(object? sender, RoutedEventArgs e)
    {
        Program.RuntimeLoader.OnRuntimeThread += AddEntity;
        void AddEntity(IRuntimeContext ctx)
        {
            Debug.Assert(ctx.SceneManager.CurrentScene != null);
            ctx.SceneManager.CurrentScene.AddEntity();
        }
    }

    private void RemoveEntity(EntityReference entity)
    {
        Program.RuntimeLoader.OnRuntimeThread += RemoveEntity;
        void RemoveEntity(IRuntimeContext ctx)
        {
            Debug.Assert(ctx.SceneManager.CurrentScene != null);
            ctx.SceneManager.CurrentScene.RemoveEntity(entity);
        }
    }

    private void UpdateChildrenCount(int neededNodesCount)
    {
        var currentNodesCount = ChildrenNodes.Count;
        var delta = currentNodesCount - neededNodesCount;
        if (delta > 0)
        {
            for (int i = 0; i < delta; i++)
            {
                ChildrenNodes[^1].Dispose();
                ChildrenNodes.RemoveAt(ChildrenNodes.Count - 1);
            }
        }
        else if (delta < 0)
        {
            for (int i = delta; i < 0; i++)
            {
                var node = _hierarchyNodeCreator.GetOrCreateNode();
                ChildrenNodes.Add(node);
            }
        }
    }

    private void SelectEntity(EntityReference entityRef)
    {
        OnEntitySelected?.Invoke(entityRef);
    }

    private void Deselect()
    {
        OnEntitySelected?.Invoke(EntityReference.Null);
    }

    private class EntityReferenceComparer : IComparer<EntityReference>
    {
        public int Compare(EntityReference e1, EntityReference e2)
        {
            return e1.Entity.Get<Order>().order.CompareTo(e2.Entity.Get<Order>().order);
        }
    }
}