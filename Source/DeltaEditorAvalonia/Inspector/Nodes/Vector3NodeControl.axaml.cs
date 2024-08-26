using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class Vector3NodeControl : UserControl, INode
{
    public Vector3NodeControl() => InitializeComponent();
    public Vector3NodeControl(NodeData nodeData) : this()
    {

    }

    public bool UpdateData(EntityReference entity)
    {
        throw new System.NotImplementedException();
    }
}