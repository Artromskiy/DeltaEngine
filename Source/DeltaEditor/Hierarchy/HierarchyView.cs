using Arch.Core;
using Delta.Runtime;
using DeltaEditor.Hierarchy;
using DeltaEditorLib.Loader;

namespace DeltaEditor;

internal class HierarchyView : ContentView
{
    public event Action<EntityReference>? OnEntitySelected;
    private readonly VerticalStackLayout _stack = [];

    public HierarchyView(RuntimeLoader runtimeLoader)
    {
        Content = _stack;
    }

    public void UpdateHierarchy(IRuntime runtime)
    {
        var entities = runtime.GetEntities();
        ResizeStack(entities.Count);
        for (int i = 0; i < _stack.Children.Count; i++)
            GetNode(i).UpdateEntity(entities[i]);
    }

    private void ResizeStack(int count)
    {
        var delta = _stack.Children.Count - count;
        for (int i = _stack.Count - 1; i > count; i--)
            RemoveNodeAt(i);
        for (int i = 0; i > delta; i--)
            AddNewNode();
    }

    private EntityNode GetNode(int index) => (_stack.Children[index] as EntityNode)!;

    private void RemoveNodeAt(int index)
    {
        GetNode(index).OnClicked -= OnClicked;
        _stack.RemoveAt(index);
    }

    private void AddNewNode()
    {
        EntityNode node = new(EntityReference.Null);
        node.OnClicked += OnClicked;
        _stack.Add(node);
    }

    private void OnClicked(EntityNode node)
    {
        for (int i = 0; i < _stack.Children.Count; i++)
            GetNode(i).Selected = false;
        node.Selected = true;
        OnEntitySelected?.Invoke(node.Entity);
    }
}