using Arch.Core;
using Delta.Runtime;
using DeltaEditorLib.Loader;
using System.Diagnostics;

namespace DeltaEditor.Hierarchy;

internal class HierarchyView : ContentView
{
    public event Action<EntityReference>? OnEntitySelected;
    private readonly VerticalStackLayout _hierarchyStack;
    private readonly Button _addNewEntity = new() { Text = "Add Entity" };
    private readonly Button _removeEntity = new() { Text = "Remove Entity" };
    private readonly VerticalStackLayout _entityNodeStack = [];
    private readonly RuntimeLoader _runtimeLoader;

    private EntityNode? _selectedNode = null;
    private EntityReference _selectedEntity = EntityReference.Null;
    public HierarchyView(RuntimeLoader runtimeLoader)
    {
        _runtimeLoader = runtimeLoader;
        _hierarchyStack = [_addNewEntity, _removeEntity, _entityNodeStack];
        _hierarchyStack.Margin = 10;
        _hierarchyStack.Spacing = 10;
        _addNewEntity.Clicked += CreateNewEntity;
        _removeEntity.Clicked += RemoveSelectedEntity;
        Content = _hierarchyStack;
    }

    public void UpdateHierarchy(IRuntime runtime)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var entities = runtime.Context.SceneManager.GetEntities();
        entities.Sort((e1, e2) => e1.Entity.Id.CompareTo(e2.Entity.Id));
        ResizeStack(entities.Count);
        for (int i = 0; i < _entityNodeStack.Children.Count; i++)
        {
            GetNode(i).UpdateEntity(entities[i]);
            GetNode(i).Selected = GetNode(i).Entity == _selectedEntity;
        }
        sw.Stop();
        var elapsed = sw.ElapsedMilliseconds;
    }

    private void CreateNewEntity(object? sender, EventArgs e)
    {
        _runtimeLoader.OnRuntimeThread += r => r.Context.SceneManager.CurrentScene.AddEntity();
    }

    private void RemoveSelectedEntity(object? sender, EventArgs e)
    {
        if (_selectedNode == null)
            return;
        var toRemove = _selectedNode.Entity;
        _runtimeLoader.OnRuntimeThread += r => r.Context.SceneManager.CurrentScene.RemoveEntity(toRemove);
        Deselect();
    }

    private EntityNode GetNode(int index) => (_entityNodeStack.Children[index] as EntityNode)!;

    private void ResizeStack(int count)
    {
        var delta = _entityNodeStack.Children.Count - count;
        for (int i = _entityNodeStack.Count - 1; i >= count; i--)
        {
            GetNode(i).OnClicked -= SelectEntity;
            _entityNodeStack.RemoveAt(i);
        }
        for (int i = 0; i > delta; i--)
        {
            EntityNode node = new(EntityReference.Null);
            node.OnClicked += SelectEntity;
            _entityNodeStack.Add(node);
        }
    }

    private void SelectEntity(EntityNode node)
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
        for (int i = 0; i < _entityNodeStack.Children.Count; i++)
            GetNode(i).Selected = false;
        _selectedNode = null;
        _selectedEntity = EntityReference.Null;
        OnEntitySelected?.Invoke(_selectedEntity);
    }
}