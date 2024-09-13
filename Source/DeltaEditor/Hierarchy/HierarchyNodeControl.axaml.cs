using Arch.Core;
using Arch.Core.Extensions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Delta.ECS.Components;
using Delta.Runtime;
using DeltaEditor.Hierarchy;
using System;
using System.Diagnostics;

namespace DeltaEditor;

public sealed partial class HierarchyNodeControl : UserControl, IDisposable
{
    public static readonly StyledProperty<bool> SelectedProperty =
        AvaloniaProperty.Register<ComponentNodeControl, bool>(nameof(Selected));

    public static readonly StyledProperty<Controls?> ComponentGridChildrenProperty =
        AvaloniaProperty.Register<ComponentNodeControl, Controls?>(nameof(Rows));

    private readonly HierarchyNodeCreator _creator;
    private IListWrapper<HierarchyNodeControl, Control> ChildrenNodes => new(ChildrenGrid.Children);

    private const string CollapsedSvgPath = "/Assets/Icons/collapsed.svg";
    private const string ExpandedSvgPath = "/Assets/Icons/expanded.svg";

    private EntityReference _entity;

    public HierarchyNodeControl() => InitializeComponent();
    public HierarchyNodeControl(HierarchyNodeCreator creator) : this()
    {
        _creator = creator;
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        this.PointerPressed += HierarchyNodeControl_PointerPressed;
        this.PointerReleased += HierarchyNodeControl_PointerReleased;
        this.PointerMoved += HierarchyNodeControl_PointerMoved;
    }

    private void HierarchyNodeControl_PointerMoved(object? sender, PointerEventArgs e)
    {
        Debug.WriteLine("Moved");
    }

    private void HierarchyNodeControl_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {

    }

    private void HierarchyNodeControl_PointerPressed(object? sender, PointerPressedEventArgs e)
    {

    }

    public Controls? Rows => ChildrenGrid.Children;


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
            ChildrenGrid.IsVisible = !value;
        }
    }

    private void OnCollapseClick(object? sender, RoutedEventArgs e) => Collapsed = !Collapsed;
    private void OnRemoveClick(object? sender, RoutedEventArgs e) => _creator?.CallRemove(_entity);
    private void OnSelectClick(object? sender, TappedEventArgs e) => _creator?.CallSelect(_entity);
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var data = e.Data;
    }
    public void UpdateEntity(IRuntimeContext ctx, EntityReference entityReference)
    {
        _entity = entityReference;
        NodeName.Content = EntityString(_entity);
        if (!Collapsed)
            UpdateChildren(ctx);
    }

    private void UpdateChildren(IRuntimeContext ctx)
    {
        var children = _creator.GetChildren(ctx, _entity);
        var count = ChildrenNodes.Count;
        UpdateChildrenCount(count);
        for (int i = 0; i < count; i++)
            ChildrenNodes[i].UpdateEntity(ctx, children[i]);
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
}