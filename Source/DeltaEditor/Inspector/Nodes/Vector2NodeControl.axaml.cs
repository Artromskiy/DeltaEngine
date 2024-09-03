using Arch.Core;
using Avalonia.Controls;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

public partial class Vector2NodeControl : UserControl, INode
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
    }
    public bool UpdateData(ref EntityReference entity)
    {
        return _nodeDataX.UpdateFloat(FieldX.FieldData, ref entity) |
               _nodeDataY.UpdateFloat(FieldY.FieldData, ref entity);
    }
}