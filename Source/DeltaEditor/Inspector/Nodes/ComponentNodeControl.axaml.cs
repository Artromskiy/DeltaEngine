using Arch.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Delta.Runtime;
using DeltaEditor.Hierarchy;
using DeltaEditor.Inspector;
using DeltaEditor.Inspector.Internal;
using System;

namespace DeltaEditor;

internal partial class ComponentNodeControl : InspectorNode
{
    public static readonly StyledProperty<Controls?> ComponentGridChildrenProperty =
        AvaloniaProperty.Register<ComponentNodeControl, Controls?>(nameof(Children));

    public static readonly StyledProperty<bool> CollapsedProperty =
        AvaloniaProperty.Register<ComponentNodeControl, bool>(nameof(Collapsed), false);

    private const string CollapsedSvgPath = "/Assets/Icons/collapsed.svg";
    private const string ExpandedSvgPath = "/Assets/Icons/expanded.svg";

    private IListWrapper<InspectorNode, Control> ChildrenNodes => new(ChildrenStack.Children);

    private readonly NodeData _nodeData;
    public Type ComponentType => _nodeData.Component;
    public event Action<Type> OnComponentRemoveRequest;

    public Controls? Children => ChildrenStack.Children;


    public bool Collapsed
    {
        get => GetValue(CollapsedProperty);
        set
        {
            SetValue(CollapsedProperty, value);
            CollapseIcon.Path = value ? CollapsedSvgPath : ExpandedSvgPath;
            ChildrenStack.IsVisible = !value;
        }
    }
    private void OnCollapseClick(object? sender, RoutedEventArgs e) => Collapsed = !Collapsed;
    private void OnRemoveClick(object? sender, RoutedEventArgs e) => OnComponentRemoveRequest.Invoke(_nodeData.Component);

    public ComponentNodeControl() => InitializeComponent();
    public ComponentNodeControl(NodeData nodeData) : this()
    {
        _nodeData = nodeData;
        ComponentName.Content = _nodeData.FieldName;
        int fieldsCount = _nodeData.FieldNames.Length;
        for (int i = 0; i < fieldsCount; i++)
        {
            var childNodeData = _nodeData.ChildData(_nodeData.FieldNames[i]);
            ChildrenNodes.Add(NodeFactory.CreateNode(childNodeData));
        }
    }

    public override bool UpdateData(ref EntityReference entity, IRuntimeContext ctx)
    {
        DebugTimer.StartDebug();

        bool changed = false;
        if (ClipVisible && !Collapsed)
            foreach (var node in ChildrenNodes)
                changed |= node.UpdateData(ref entity, ctx);

        DebugTimer.StopDebug();
        return changed;
    }

    public override void SetLabelColor(IBrush brush) => ComponentName.Foreground = brush;
}