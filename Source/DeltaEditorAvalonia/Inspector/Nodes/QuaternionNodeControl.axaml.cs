using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class QuaternionNodeControl : UserControl, INode
{
    public QuaternionNodeControl() => InitializeComponent();
    public QuaternionNodeControl(NodeData nodeData):this()
    {

    }
    public bool UpdateData(EntityReference entity)
    {
        throw new System.NotImplementedException();
    }
}