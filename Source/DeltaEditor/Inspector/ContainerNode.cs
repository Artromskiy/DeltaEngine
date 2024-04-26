using Arch.Core;

namespace DeltaEditor.Inspector
{
    internal class ContainerNode : Node
    {
        private readonly StackLayout _stack;

        private readonly List<INode> _inspectorElements;

        public ContainerNode(NodeData parameters) : base(parameters)
        {
            _stack = [_fieldName];
            _stack.Margin = 5;
            _stack.BackgroundColor = NodeConst.BackColor;
            _inspectorElements = [];
            foreach (var item in _nodeData.FieldNames)
            {
                var element = NodeFactory.CreateNode(_nodeData.ChildData(item));
                _inspectorElements.Add(element);
                _stack.Add(element);
            }
            Content = _stack;
        }

        public override void UpdateData(EntityReference entity)
        {
            foreach (var inspectorElement in _inspectorElements)
                inspectorElement.UpdateData(entity);
        }
    }
}
