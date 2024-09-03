using Arch.Core;
using Avalonia.Controls;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

public partial class IntNodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    public IntNodeControl() => InitializeComponent();
    public IntNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
    }
    public bool UpdateData(ref EntityReference entity) => _nodeData.UpdateInt(Field.FieldData, ref entity);
}