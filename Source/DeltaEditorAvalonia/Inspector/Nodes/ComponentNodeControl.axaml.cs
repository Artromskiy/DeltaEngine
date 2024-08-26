using Arch.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DeltaEditorAvalonia.Inspector;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

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
        //FieldName.Content = nodeData.FieldName;
        int fieldsCount = nodeData.FieldNames.Length;
        ChildrenGrid.ColumnDefinitions = [];
        for (int i = 0; i < fieldsCount; i++)
            ChildrenGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        for (int i = 0; i < fieldsCount; i++)
        {
            var element = (Control)NodeFactory.CreateNode(nodeData.ChildData(nodeData.FieldNames[i]));
            element[Grid.RowProperty] = i;
            ChildrenGrid.Children.Add(element);
        }
    }
    public bool UpdateData(EntityReference entity)
    {
        throw new System.NotImplementedException();
    }

}