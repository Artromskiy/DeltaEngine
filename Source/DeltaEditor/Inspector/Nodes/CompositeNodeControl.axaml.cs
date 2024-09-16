using Arch.Core;
using Avalonia.Controls;
using Avalonia.Media;
using DeltaEditor.Hierarchy;
using DeltaEditor.Inspector;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

internal partial class CompositeNodeControl : InspectorNode
{
    private IListWrapper<InspectorNode, Control> ChildrenNodes => new(ChildrenStack.Children);
    public CompositeNodeControl() => InitializeComponent();

    public CompositeNodeControl(NodeData nodeData) : this()
    {
        FieldName.Content = nodeData.FieldName;
        int fieldsCount = nodeData.FieldNames.Length;
        for (int i = 0; i < fieldsCount; i++)
        {
            var childNodeData = nodeData.ChildData(nodeData.FieldNames[i]);
            ChildrenNodes.Add(NodeFactory.CreateNode(childNodeData));
        }
    }

    public override bool UpdateData(ref EntityReference entity)
    {
        if (!ClipVisible)
            return false;

        bool changed = false;
        for (int i = 0; i < ChildrenNodes.Count; i++)
            changed |= ChildrenNodes[i].UpdateData(ref entity);


        return changed;
    }

    public override void SetLabelColor(IBrush brush) => FieldName.Foreground = brush;
}