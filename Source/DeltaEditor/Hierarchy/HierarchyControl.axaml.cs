using Arch.Core;
using Avalonia.Controls;
using Delta.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeltaEditor;

public partial class HierarchyControl : UserControl
{
    public event Action<EntityReference>? OnEntitySelected;
    private HierarchyNodeControl? _selectedNode = null;
    private EntityReference _selectedEntity = EntityReference.Null;
    private readonly IComparer<EntityReference> _entityReferenceComparer = new EntityReferenceComparer();
    private readonly Stopwatch sw = new();
    private int prevTime = 0;

    public HierarchyControl()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
            return;
    }
    public void UpdateHierarchy(IRuntimeContext ctx)
    {
        sw.Restart();

        var entities = ctx.SceneManager.GetEntities();
        int count = entities.Count;
        entities.Sort(_entityReferenceComparer);
        int selectedId = entities.BinarySearch(0, count, _selectedEntity, _entityReferenceComparer);

        ResizeStack(count);

        for (int i = 0; i < count; i++)
            GetNode(i).UpdateEntity(entities[i]);

        SelectEntity(GetNode(selectedId));

        StopDebug();
    }
    private void StopDebug()
    {
        sw.Stop();
        prevTime = SmoothInt(prevTime, (int)sw.Elapsed.TotalMicroseconds, 50);
        DebugTimer.Content = $"{prevTime}us";
    }

    private static int SmoothInt(int value1, int value2, int smoothing)
    {
        return ((value1 * smoothing) + value2) / (smoothing + 1);
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
        if (_selectedNode != null)
            _selectedNode.Selected = false;
        _selectedNode = null;
        _selectedEntity = EntityReference.Null;
        OnEntitySelected?.Invoke(_selectedEntity);
    }

    private class EntityReferenceComparer : IComparer<EntityReference>
    {
        public int Compare(EntityReference e1, EntityReference e2) => e1.Entity.Id.CompareTo(e2.Entity.Id);
    }
}