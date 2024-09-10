using Arch.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DeltaEditor.Inspector;
using DeltaEditor.Inspector.Internal;
using System;
using System.Diagnostics;

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

    private const string CollapseSvgPath = "/Assets/Icons/collapse.svg";
    private const string ExpandSvgPath = "/Assets/Icons/expand.svg";

    private readonly INode[] _fields;
    private readonly NodeData _nodeData;
    public event Action<Type> OnComponentRemoveRequest;

    private readonly Stopwatch _perfWatch = new();


    public Grid? ComponentGrid
    {
        get => ChildrenGrid;
        set => ChildrenGrid = value;
    }

    public Controls? Rows => ChildrenGrid.Children;

    public RowDefinitions? RowsDefs
    {
        get => ChildrenGrid.RowDefinitions;
        set => ChildrenGrid.RowDefinitions = value;
    }

    public bool Collapsed
    {
        get => GetValue(CollapsedProperty);
        set
        {
            SetValue(CollapsedProperty, value);
            CollapseIcon.Path = value ? CollapseSvgPath : ExpandSvgPath;
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

    private int prevTime;
    public bool UpdateData(ref EntityReference entity)
    {
        _perfWatch.Restart();

        bool changed = false;
        if (!Collapsed)
            for (int i = 0; i < _fields.Length; i++)
                changed |= _fields[i].UpdateData(ref entity);

        StopDebug();

        return changed;
    }

    private void StopDebug()
    {
        _perfWatch.Stop();
        prevTime = Helpers.SmoothInt(prevTime, (int)_perfWatch.Elapsed.TotalMicroseconds, 50);
        DebugTimer.Content = $"{prevTime}us";
    }
}