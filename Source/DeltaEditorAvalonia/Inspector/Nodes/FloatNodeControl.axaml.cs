using Arch.Core;
using Avalonia.Controls;
using DeltaEditor.Tools;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class FloatNodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    public FloatNodeControl() => InitializeComponent();
    public FloatNodeControl(NodeData nodeData) : this()
    {

    }
    public bool UpdateData(EntityReference entity) => _nodeData.UpdateFloat(FieldData, entity);
}