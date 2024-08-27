using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class Vector3NodeControl : UserControl, INode
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
    }

    public bool UpdateData(EntityReference entity)
    {
        return _nodeDataX.UpdateFloat(FieldX.FieldData, entity) |
               _nodeDataY.UpdateFloat(FieldY.FieldData, entity) |
               _nodeDataZ.UpdateFloat(FieldZ.FieldData, entity);
    }
}