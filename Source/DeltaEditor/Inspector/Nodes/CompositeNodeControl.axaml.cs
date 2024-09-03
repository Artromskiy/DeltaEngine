using Arch.Core;
using Avalonia.Controls;
using DeltaEditor.Inspector;
using DeltaEditor.Inspector.Internal;
using System.Collections.Generic;

namespace DeltaEditor;

public partial class CompositeNodeControl : UserControl, INode
{
    private readonly INode[] _fields;
    public CompositeNodeControl() => InitializeComponent();

    public CompositeNodeControl(NodeData nodeData) : this()
    {
        FieldName.Content = nodeData.FieldName;
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

    public bool UpdateData(ref EntityReference entity)
    {
        bool changed = false;
        for (int i = 0; i < _fields.Length; i++)
            changed |= _fields[i].UpdateData(ref entity);

        return changed;
    }
}