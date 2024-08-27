using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class StringNodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    public StringNodeControl() => InitializeComponent();
    public StringNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
    }
    public bool UpdateData(EntityReference entity) =>  _nodeData.UpdateString(Field.FieldData, entity);
}