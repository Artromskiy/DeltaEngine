using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class Vector3NodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    public Vector3NodeControl() => InitializeComponent();
    public Vector3NodeControl(NodeData nodeData) : this()
    {
        _nodeData = nodeData;
    }

    public bool UpdateData(EntityReference entity)
    {
        return _nodeData.UpdateFloat(FieldDataX, entity) |
               _nodeData.UpdateFloat(FieldDataY, entity) |
               _nodeData.UpdateFloat(FieldDataZ, entity);
    }
}