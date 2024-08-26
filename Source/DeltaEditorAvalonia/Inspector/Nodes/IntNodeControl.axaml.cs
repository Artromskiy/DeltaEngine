using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class IntNodeControl : UserControl, INode
{
    public IntNodeControl() => InitializeComponent();
    public IntNodeControl(NodeData nodeData) : this()
    {

    }
    public bool UpdateData(EntityReference entity)
    {
        throw new System.NotImplementedException();
    }
}