using Arch.Core;
using Avalonia.Controls;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

public partial class Vector4NodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    private readonly NodeData _nodeDataX;
    private readonly NodeData _nodeDataY;
    private readonly NodeData _nodeDataZ;
    private readonly NodeData _nodeDataW;

    public Vector4NodeControl() => InitializeComponent();
    public Vector4NodeControl(NodeData nodeData) : this()
    {
        FieldName.Content = (_nodeData = nodeData).FieldName;

        _nodeDataX = _nodeData.ChildData(_nodeData.FieldNames[0]);
        _nodeDataY = _nodeData.ChildData(_nodeData.FieldNames[1]);
        _nodeDataZ = _nodeData.ChildData(_nodeData.FieldNames[2]);
        _nodeDataW = _nodeData.ChildData(_nodeData.FieldNames[3]);
    }
    public bool UpdateData(EntityReference entity)
    {
        return _nodeDataX.UpdateFloat(FieldX.FieldData, entity) |
               _nodeDataY.UpdateFloat(FieldY.FieldData, entity) |
               _nodeDataZ.UpdateFloat(FieldZ.FieldData, entity) |
               _nodeDataW.UpdateFloat(FieldW.FieldData, entity);
    }
}