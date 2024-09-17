using Arch.Core;
using Avalonia.Media;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

internal partial class IntNodeControl : InspectorNode
{
    private readonly NodeData _nodeData;
    public IntNodeControl() => InitializeComponent();
    public IntNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
    }

    public override void SetLabelColor(IBrush brush) => Field.SetFieldColor(brush);

    public override bool UpdateData(ref EntityReference entity)
    {
        if (!ClipVisible)
            return false;

        bool changed = _nodeData.UpdateInt(Field.FieldData, ref entity);

        return changed;
    }
}