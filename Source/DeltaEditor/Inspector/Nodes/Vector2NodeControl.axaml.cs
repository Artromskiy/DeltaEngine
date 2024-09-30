using Arch.Core;
using Avalonia.Media;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

internal partial class Vector2NodeControl : InspectorNode
{
    private readonly NodeData _nodeData;
    private readonly NodeData _nodeDataX;
    private readonly NodeData _nodeDataY;

    public Vector2NodeControl() => InitializeComponent();
    public Vector2NodeControl(NodeData nodeData) : this()
    {
        FieldName.Content = (_nodeData = nodeData).FieldName;

        _nodeDataX = _nodeData.ChildData(_nodeData.FieldNames[0]);
        _nodeDataY = _nodeData.ChildData(_nodeData.FieldNames[1]);

        FieldX.OnDrag += x => _nodeData.DragFloat(FieldX.FieldData, x,0.01f);
        FieldY.OnDrag += x => _nodeData.DragFloat(FieldY.FieldData, x,0.01f);
    }

    public override void SetLabelColor(IBrush brush) => FieldName.Foreground = brush;

    public override bool UpdateData(ref EntityReference entity)
    {
        if (!ClipVisible)
            return false;

        bool changed = _nodeDataX.UpdateFloat(FieldX.FieldData, ref entity) |
                      _nodeDataY.UpdateFloat(FieldY.FieldData, ref entity);

        return changed;
    }
}