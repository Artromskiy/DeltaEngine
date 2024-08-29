using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class Matrix4NodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;

    private readonly NodeData _nodeDataM11;
    private readonly NodeData _nodeDataM12;
    private readonly NodeData _nodeDataM13;
    private readonly NodeData _nodeDataM14;

    private readonly NodeData _nodeDataM21;
    private readonly NodeData _nodeDataM22;
    private readonly NodeData _nodeDataM23;
    private readonly NodeData _nodeDataM24;

    private readonly NodeData _nodeDataM31;
    private readonly NodeData _nodeDataM32;
    private readonly NodeData _nodeDataM33;
    private readonly NodeData _nodeDataM34;

    private readonly NodeData _nodeDataM41;
    private readonly NodeData _nodeDataM42;
    private readonly NodeData _nodeDataM43;
    private readonly NodeData _nodeDataM44;
    public Matrix4NodeControl() => InitializeComponent();
    public Matrix4NodeControl(NodeData nodeData) : this()
    {
        FieldName.Content = (_nodeData = nodeData).FieldName;
        _nodeDataM11 = _nodeData.ChildData(_nodeData.FieldNames[0]);
        _nodeDataM12 = _nodeData.ChildData(_nodeData.FieldNames[1]);
        _nodeDataM13 = _nodeData.ChildData(_nodeData.FieldNames[2]);
        _nodeDataM14 = _nodeData.ChildData(_nodeData.FieldNames[3]);
        _nodeDataM21 = _nodeData.ChildData(_nodeData.FieldNames[4]);
        _nodeDataM22 = _nodeData.ChildData(_nodeData.FieldNames[5]);
        _nodeDataM23 = _nodeData.ChildData(_nodeData.FieldNames[6]);
        _nodeDataM24 = _nodeData.ChildData(_nodeData.FieldNames[7]);
        _nodeDataM31 = _nodeData.ChildData(_nodeData.FieldNames[8]);
        _nodeDataM32 = _nodeData.ChildData(_nodeData.FieldNames[9]);
        _nodeDataM33 = _nodeData.ChildData(_nodeData.FieldNames[10]);
        _nodeDataM34 = _nodeData.ChildData(_nodeData.FieldNames[11]);
        _nodeDataM41 = _nodeData.ChildData(_nodeData.FieldNames[12]);
        _nodeDataM42 = _nodeData.ChildData(_nodeData.FieldNames[13]);
        _nodeDataM43 = _nodeData.ChildData(_nodeData.FieldNames[14]);
        _nodeDataM44 = _nodeData.ChildData(_nodeData.FieldNames[15]);
    }

    public bool UpdateData(EntityReference entity)
    {
        return _nodeDataM11.UpdateFloat(FieldM11.FieldData, entity) |
               _nodeDataM12.UpdateFloat(FieldM12.FieldData, entity) |
               _nodeDataM13.UpdateFloat(FieldM13.FieldData, entity) |
               _nodeDataM14.UpdateFloat(FieldM14.FieldData, entity) |
               _nodeDataM21.UpdateFloat(FieldM21.FieldData, entity) |
               _nodeDataM22.UpdateFloat(FieldM22.FieldData, entity) |
               _nodeDataM23.UpdateFloat(FieldM23.FieldData, entity) |
               _nodeDataM24.UpdateFloat(FieldM24.FieldData, entity) |
               _nodeDataM31.UpdateFloat(FieldM31.FieldData, entity) |
               _nodeDataM32.UpdateFloat(FieldM32.FieldData, entity) |
               _nodeDataM33.UpdateFloat(FieldM33.FieldData, entity) |
               _nodeDataM34.UpdateFloat(FieldM34.FieldData, entity) |
               _nodeDataM41.UpdateFloat(FieldM41.FieldData, entity) |
               _nodeDataM42.UpdateFloat(FieldM42.FieldData, entity) |
               _nodeDataM43.UpdateFloat(FieldM43.FieldData, entity) |
               _nodeDataM44.UpdateFloat(FieldM44.FieldData, entity);
    }
}