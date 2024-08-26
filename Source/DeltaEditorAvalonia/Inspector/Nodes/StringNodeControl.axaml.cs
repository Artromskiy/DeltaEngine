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
        _nodeData = nodeData;
    }
    public bool UpdateData(EntityReference entity) => _nodeData.UpdateString(FieldData, entity);
}