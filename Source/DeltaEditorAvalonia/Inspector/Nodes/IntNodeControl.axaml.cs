using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class IntNodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    public IntNodeControl() => InitializeComponent();
    public IntNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
    }
    public bool UpdateData(EntityReference entity) => _nodeData.UpdateInt(Field.FieldData, entity);
}