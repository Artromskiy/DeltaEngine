using Arch.Core;
using Avalonia.Media;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

internal partial class Vector3NodeControl : InspectorNode
{
    private readonly NodeData _nodeData;
    private readonly NodeData _nodeDataX;
    private readonly NodeData _nodeDataY;
    private readonly NodeData _nodeDataZ;
    public Vector3NodeControl() => InitializeComponent();
    public Vector3NodeControl(NodeData nodeData) : this()
    {
        FieldName.Content = (_nodeData = nodeData).FieldName;
        _nodeDataX = _nodeData.ChildData(_nodeData.FieldNames[0]);
        _nodeDataY = _nodeData.ChildData(_nodeData.FieldNames[1]);
        _nodeDataZ = _nodeData.ChildData(_nodeData.FieldNames[2]);
        FieldX.OnDrag += x => _nodeData.DragFloat(FieldX.FieldData, x, 0.01f);
        FieldY.OnDrag += x => _nodeData.DragFloat(FieldY.FieldData, x, 0.01f);
        FieldZ.OnDrag += x => _nodeData.DragFloat(FieldZ.FieldData, x, 0.01f);

    }

    public override void SetLabelColor(IBrush brush) => FieldName.Foreground = brush;

    public override bool UpdateData(ref EntityReference entity, IRuntimeContext ctx)
    {
        if (!ClipVisible)
            return false;

        bool changed = _nodeDataX.UpdateFloat(FieldX.FieldData, ref entity) |
                      _nodeDataY.UpdateFloat(FieldY.FieldData, ref entity) |
                      _nodeDataZ.UpdateFloat(FieldZ.FieldData, ref entity);

        return changed;
    }
}