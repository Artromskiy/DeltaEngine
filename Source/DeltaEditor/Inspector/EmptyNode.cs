using Arch.Core;

namespace DeltaEditor.Inspector
{
    internal class EmptyNode : Node
    {
        public EmptyNode(NodeData nodeData) : base(nodeData)
        {
            Content = _fieldName;
        }
        public override void UpdateData(EntityReference entity) { }
    }
}
