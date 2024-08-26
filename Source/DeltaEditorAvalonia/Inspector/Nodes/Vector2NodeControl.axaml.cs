using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class Vector2NodeControl : UserControl, INode
{
    public Vector2NodeControl()=> InitializeComponent();
    public Vector2NodeControl(NodeData nodeData):this()
    {

    }
    public bool UpdateData(EntityReference entity)
    {
        throw new System.NotImplementedException();
    }
}