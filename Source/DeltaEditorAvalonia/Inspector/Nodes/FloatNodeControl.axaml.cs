using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class FloatNodeControl : UserControl, INode
{
    public FloatNodeControl() => InitializeComponent();

    public FloatNodeControl(NodeData nodeData) : this()
    {

    }
    public bool UpdateData(EntityReference entity)
    {
        throw new System.NotImplementedException();
    }
}