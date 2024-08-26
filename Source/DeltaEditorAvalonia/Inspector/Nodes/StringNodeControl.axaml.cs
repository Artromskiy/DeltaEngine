using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class StringNodeControl : UserControl, INode
{
    public StringNodeControl() => InitializeComponent();
    public StringNodeControl(NodeData nodeData) : this()
    {

    }
    public bool UpdateData(EntityReference entity)
    {
        throw new System.NotImplementedException();
    }
}