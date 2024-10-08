using Arch.Core;
using Arch.Core.Extensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Delta.ECS;
using Delta.ECS.Components;
using Delta.Runtime;
using DeltaEditor.Hierarchy;
using System;

namespace DeltaEditor;

public sealed partial class HierarchyNodeControl : UserControl, IDisposable
{
    public static readonly StyledProperty<bool> SelectedProperty =
        AvaloniaProperty.Register<ComponentNodeControl, bool>(nameof(Selected));

    public static readonly StyledProperty<Controls?> ChildrenProperty =
        AvaloniaProperty.Register<ComponentNodeControl, Controls?>(nameof(ContentChildren));

    private readonly HierarchyNodeCreator _creator;
    private IListWrapper<HierarchyNodeControl, Control> ChildrenNodes => new(ChildrenStack.Children);

    private const string CollapsedSvgPath = "/Assets/Icons/collapsed.svg";
    private const string ExpandedSvgPath = "/Assets/Icons/expanded.svg";

    private EntityReference _entity;

    public HierarchyNodeControl() => InitializeComponent();
    public HierarchyNodeControl(HierarchyNodeCreator creator) : this()
    {
        _creator = creator;
    }

    public Controls? ContentChildren => ChildrenStack.Children;


    public bool Selected
    {
        get => GetValue(SelectedProperty);
        set
        {
            if (GetValue(SelectedProperty) == value)
                return;
            SetValue(SelectedProperty, value);
            Background = new SolidColorBrush(value ? Colors.Cyan : Colors.Magenta);
        }
    }

    public bool Collapsed
    {
        get => _creator?.IsCollapsed(_entity) ?? true;
        set
        {
            _creator?.SetCollapsed(_entity, value);
            CollapseIcon.Path = value ? CollapsedSvgPath : ExpandedSvgPath;
            ChildrenStack.IsVisible = !value;
        }
    }

    private void OnCollapseClick(object? sender, RoutedEventArgs e) => Collapsed = !Collapsed;
    private void OnRemoveClick(object? sender, RoutedEventArgs e) => _creator?.CallRemove(_entity);
    private void OnSelectClick(object? sender, TappedEventArgs e) => _creator?.CallSelect(_entity);
    public void UpdateEntity(EntityReference entityReference)
    {
        _entity = entityReference;
        NodeName.Content = EntityString(_entity);
        var count = _creator.GetChildrenCount(_entity);
        Collapsed |= count == 0;
        CollapseButton.IsVisible = count != 0;
        if (!Collapsed)
            UpdateChildren();
    }

    private void UpdateChildren()
    {
        var children = _creator.GetChildren(_entity);
        var count = children.Length;
        UpdateChildrenCount(count);
        for (int i = 0; i < count; i++)
            ChildrenNodes[i].UpdateEntity(children[i]);
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
                var node = _creator.GetOrCreateNode();
                ChildrenNodes.Add(node);
            }
        }
    }

    public void Dispose()
    {
        foreach (var item in ChildrenNodes)
            item.Dispose();
        _creator.ReturnNode(this);
    }

    private static string EntityString(EntityReference entityReference)
    {
        if (!entityReference.IsAlive())
            return string.Empty;
        var entity = entityReference.Entity;
        if (entity.TryGet<EntityName>(out var entityName) && !string.IsNullOrEmpty(entityName.name))
            return entityName.name;
        return entityReference.LookupString();
    }

    private void OnPointerEntered(object? sender, PointerEventArgs e) => RemoveButton.IsVisible = true;

    private void OnPointerExited(object? sender, PointerEventArgs e) => RemoveButton.IsVisible = false;
}