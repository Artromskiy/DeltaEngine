using Arch.Core;

namespace DeltaEditor.Inspector
{
    internal class ContainerNode : Node
    {
        private readonly StackLayout _elements;

        private readonly List<INode> _inspectorElements;

        public ContainerNode(NodeData parameters) : base(parameters)
        {
            _elements = [_fieldName];
            _elements.Padding = new Thickness(3);
            _inspectorElements = [];
            foreach (var item in _nodeData.FieldNames)
            {
                var element = NodeFactory.CreateNode(_nodeData.ChildData(item));
                _inspectorElements.Add(element);
                _elements.Add(element);
            }
            Content = _elements;
        }

        public override void UpdateData(EntityReference entity)
        {
            foreach (var inspectorElement in _inspectorElements)
                inspectorElement.UpdateData(entity);
        }
    }
}
