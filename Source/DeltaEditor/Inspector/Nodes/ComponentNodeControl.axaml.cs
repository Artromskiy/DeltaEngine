using Arch.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DeltaEditor.Inspector;
using DeltaEditor.Inspector.Internal;
using System.Collections.Generic;
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
        AvaloniaProperty.Register<ComponentNodeControl, bool>(nameof(Collapsed));

    private const string CollapseSvgPath = "/Assets/Icons/collapse.svg";
    private const string ExpandSvgPath = "/Assets/Icons/expand.svg";

    private readonly Stopwatch _perfWatch = new();

    private readonly INode[] _fields;

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

    public ComponentNodeControl() => InitializeComponent();
    public ComponentNodeControl(NodeData nodeData) : this()
    {
        ComponentName.Content = nodeData.FieldName;
        int fieldsCount = nodeData.FieldNames.Length;
        _fields = new INode[fieldsCount];
        ChildrenGrid.RowDefinitions = [];
        for (int i = 0; i < fieldsCount; i++)
            ChildrenGrid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
        for (int i = 0; i < fieldsCount; i++)
        {
            var node = NodeFactory.CreateNode(nodeData.ChildData(nodeData.FieldNames[i]));
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

        _perfWatch.Stop();
        prevTime = SmoothInt(prevTime, (int)_perfWatch.Elapsed.TotalMicroseconds, 50);
        DebugTimer.Content = $"{prevTime}us";

        return changed;
    }

    private void StopDebug()
    {
        _perfWatch.Stop();
        prevTime = SmoothInt(prevTime, (int)_perfWatch.Elapsed.TotalMicroseconds, 50);
        DebugTimer.Content = $"{prevTime}us";
    }

    private static int SmoothInt(int value1, int value2, int smoothing)
    {
        return ((value1 * smoothing) + value2) / (smoothing + 1);
    }
}