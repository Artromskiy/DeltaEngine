using Arch.Core;
using Avalonia.Controls;
using DeltaEditor.Tools;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class IntNodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    public IntNodeControl() => InitializeComponent();
    public IntNodeControl(NodeData nodeData) : this()
    {
        _nodeData = nodeData;
    }
    public bool UpdateData(EntityReference entity) => _nodeData.UpdateInt(FieldData, entity);
}