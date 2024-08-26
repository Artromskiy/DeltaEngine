using Arch.Core;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DeltaEditorAvalonia.Inspector.Internal;

namespace DeltaEditorAvalonia;

public partial class Vector4NodeControl : UserControl, INode
{
    public Vector4NodeControl()=> InitializeComponent();
    public Vector4NodeControl(NodeData nodeData):this()
    {

    }
    public bool UpdateData(EntityReference entity)
    {
        throw new System.NotImplementedException();
    }
}