using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class FloatNodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    public FloatNodeControl() => InitializeComponent();
    public FloatNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
    }
    public bool UpdateData(EntityReference entity) => _nodeData.UpdateFloat(Field.FieldData, entity);
}