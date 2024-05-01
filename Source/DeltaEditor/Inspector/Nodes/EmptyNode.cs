using Arch.Core;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor.Inspector.Nodes;


internal class EmptyNode : Node
{
    public EmptyNode(NodeData nodeData) : base(nodeData)
    {
        Content = _fieldName;
    }
    public override void UpdateData(EntityReference entity) { }
}
