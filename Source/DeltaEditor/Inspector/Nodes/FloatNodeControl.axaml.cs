using Arch.Core;
using Avalonia.Media;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

internal partial class FloatNodeControl : InspectorNode
{
    private readonly NodeData _nodeData;
    public FloatNodeControl() => InitializeComponent();
    public FloatNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
    }

    public override void SetLabelColor(IBrush brush)=> Field.SetFieldColor(brush);

    public override bool UpdateData(ref EntityReference entity)
    {
        if (!ClipVisible)
            return false;

        bool changed = _nodeData.UpdateFloat(Field.FieldData, ref entity);

        return changed;
    }
}