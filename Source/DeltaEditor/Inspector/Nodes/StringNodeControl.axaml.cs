using Arch.Core;
using Avalonia.Media;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

internal partial class StringNodeControl : InspectorNode
{
    private readonly NodeData _nodeData;
    public StringNodeControl() => InitializeComponent();
    public StringNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
    }

    public override void SetLabelColor(IBrush brush) {}

    public override bool UpdateData(ref EntityReference entity, IRuntimeContext ctx)
    {
        if (!ClipVisible)
            return false;
        bool changed = _nodeData.UpdateString(Field.FieldData, ref entity);

        return changed;
    }
}