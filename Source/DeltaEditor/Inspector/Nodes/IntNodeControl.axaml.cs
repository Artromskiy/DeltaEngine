using Arch.Core;
using Avalonia.Media;
using DVG.Engine.Runtime;
using DVG.Engine.Editor.Inspector.Internal;

namespace DVG.Engine.Editor;

internal partial class IntNodeControl : InspectorNode
{
    private readonly NodeData _nodeData;
    public IntNodeControl() => InitializeComponent();
    public IntNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
        Field.OnDrag += x => _nodeData.DragInt(Field.FieldData, x);
    }

    public override void SetLabelColor(IBrush brush) {}

    public override bool UpdateData(ref EntityReference entity)
    {
        if (!ClipVisible)
            return false;

        bool changed = _nodeData.UpdateInt(Field.FieldData, ref entity);

        return changed;
    }
}
