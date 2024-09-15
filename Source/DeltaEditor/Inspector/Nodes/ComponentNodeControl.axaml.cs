using Arch.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DeltaEditor.Inspector;
using DeltaEditor.Inspector.Internal;
using System;

namespace DeltaEditor;

internal partial class ComponentNodeControl : UserControl, INode
{
    public static readonly StyledProperty<Grid?> ComponentGridProperty =
        AvaloniaProperty.Register<ComponentNodeControl, Grid?>(nameof(ComponentGrid));

    public static readonly StyledProperty<RowDefinitions?> GridRowsProperty =
        AvaloniaProperty.Register<ComponentNodeControl, RowDefinitions?>(nameof(RowsDefs));

    public static readonly StyledProperty<Controls?> ComponentGridChildrenProperty =
        AvaloniaProperty.Register<ComponentNodeControl, Controls?>(nameof(Rows));

    public static readonly StyledProperty<bool> CollapsedProperty =
        AvaloniaProperty.Register<ComponentNodeControl, bool>(nameof(Collapsed), false);

    private const string CollapsedSvgPath = "/Assets/Icons/collapsed.svg";
    private const string ExpandedSvgPath = "/Assets/Icons/expanded.svg";

    private readonly INode[] _fields;
    private readonly NodeData _nodeData;
    public event Action<Type> OnComponentRemoveRequest;



    public Controls? Rows => ChildrenGrid.Children;
    public Grid? ComponentGrid
    {
        get => ChildrenGrid;
        set => ChildrenGrid = value;
    }

    public RowDefinitions? RowsDefs
    {
        get => ChildrenGrid.RowDefinitions;
        set => ChildrenGrid.RowDefinitions = value ?? ChildrenGrid.RowDefinitions;
    }

    public bool Collapsed
    {
        get => GetValue(CollapsedProperty);
        set
        {
            SetValue(CollapsedProperty, value);
            CollapseIcon.Path = value ? CollapsedSvgPath : ExpandedSvgPath;
            MainGrid.RowDefinitions[1].Height = value ? new GridLength(0) : GridLength.Star;
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
        _fields = new INode[fieldsCount];
        ChildrenGrid.RowDefinitions = [];
        for (int i = 0; i < fieldsCount; i++)
            ChildrenGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
        for (int i = 0; i < fieldsCount; i++)
        {
            var node = NodeFactory.CreateNode(_nodeData.ChildData(_nodeData.FieldNames[i]));
            var control = (Control)node;
            _fields[i] = node;
            control[Grid.RowProperty] = i;
            ChildrenGrid.Children.Add(control);
        }
    }

    public bool UpdateData(ref EntityReference entity)
    {
        DebugTimer.StartDebug();

        bool changed = false;
        if (!Collapsed)
            for (int i = 0; i < _fields.Length; i++)
                changed |= _fields[i].UpdateData(ref entity);

        DebugTimer.StopDebug();

        return changed;
    }
}