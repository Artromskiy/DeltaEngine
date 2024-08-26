using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class CompositeNodeControl : UserControl, INode
{
    public CompositeNodeControl() => InitializeComponent();

    public CompositeNodeControl(NodeData nodeData) : this()
    {
        FieldName.Content = nodeData.FieldName;
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