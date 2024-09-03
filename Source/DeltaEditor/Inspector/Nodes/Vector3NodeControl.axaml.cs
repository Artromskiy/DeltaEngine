using Arch.Core;
using Avalonia.Controls;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

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

    public bool UpdateData(ref EntityReference entity)
    {
        return _nodeDataX.UpdateFloat(FieldX.FieldData, ref entity) |
               _nodeDataY.UpdateFloat(FieldY.FieldData, ref entity) |
               _nodeDataZ.UpdateFloat(FieldZ.FieldData, ref entity);
    }
}