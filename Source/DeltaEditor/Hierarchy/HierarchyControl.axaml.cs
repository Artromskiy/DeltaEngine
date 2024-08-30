using Arch.Core;
using Avalonia.Controls;
using Delta.Runtime;
using DeltaEditor;
using System;

namespace DeltaEditor;

public partial class HierarchyControl : UserControl
{
    public event Action<EntityReference>? OnEntitySelected;
    private HierarchyNodeControl? _selectedNode = null;
    private EntityReference _selectedEntity = EntityReference.Null;

    public HierarchyControl()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
            return;
    }

    public void UpdateHierarchy(IRuntimeContext ctx)
    {
        var entities = ctx.SceneManager.GetEntities();
        entities.Sort((e1, e2) => e1.Entity.Id.CompareTo(e2.Entity.Id));
        ResizeStack(entities.Count);
        for (int i = 0; i < EntityNodeStack.Children.Count; i++)
        {
            GetNode(i).UpdateEntity(entities[i]);
            GetNode(i).Selected = entities[i] == _selectedEntity;
        }
    }

    private void CreateNewEntity(object? sender, EventArgs e)
    {
        Program.RuntimeLoader.OnRuntimeThread += ctx => ctx.SceneManager.CurrentScene.AddEntity();
    }

    private void RemoveSelectedEntity(object? sender, EventArgs e)
    {
        if (_selectedEntity == EntityReference.Null)
            return;
        var toRemove = _selectedEntity;
        Deselect();
        Program.RuntimeLoader.OnRuntimeThread += ctx => ctx.SceneManager.CurrentScene.RemoveEntity(toRemove);
    }

    private HierarchyNodeControl GetNode(int index) => (EntityNodeStack.Children[index] as HierarchyNodeControl)!;

    private void ResizeStack(int count)
    {
        var delta = EntityNodeStack.Children.Count - count;
        for (int i = EntityNodeStack.Children.Count - 1; i >= count; i--)
        {
            GetNode(i).OnClicked -= SelectEntity;
            EntityNodeStack.Children.RemoveAt(i);
        }
        for (int i = 0; i > delta; i--)
        {
            HierarchyNodeControl node = new(EntityReference.Null);
            node.OnClicked += SelectEntity;
            EntityNodeStack.Children.Add(node);
        }
    }

    private void SelectEntity(HierarchyNodeControl node)
    {
        if (_selectedNode != null)
            _selectedNode.Selected = false;

        _selectedNode = node;
        _selectedNode.Selected = true;
        _selectedEntity = _selectedNode.Entity;
        OnEntitySelected?.Invoke(_selectedEntity);
    }

    private void Deselect()
    {
        for (int i = 0; i < EntityNodeStack.Children.Count; i++)
            GetNode(i).Selected = false;
        _selectedNode = null;
        _selectedEntity = EntityReference.Null;
        OnEntitySelected?.Invoke(_selectedEntity);
    }
}