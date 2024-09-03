using Arch.Core;
using Avalonia.Controls;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

public partial class FloatNodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    public FloatNodeControl() => InitializeComponent();
    public FloatNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
    }
    public bool UpdateData(ref EntityReference entity) => _nodeData.UpdateFloat(Field.FieldData, ref entity);
}