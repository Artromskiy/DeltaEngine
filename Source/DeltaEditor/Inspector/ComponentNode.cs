using Arch.Core;

namespace DeltaEditor.Inspector
{
    internal class ComponentNode : Node
    {
        private readonly Label _componentName;
        private readonly StackLayout _field;
        protected sealed override bool SuppressTypeCheck => true;

        private readonly List<INode> _inspectorElements;

        public ComponentNode(NodeData parameters):base(parameters)
        {
            _componentName = new() { Text = parameters.Component.Name };
            _field = [_componentName];
            _inspectorElements = [];
            foreach (var item in _nodeData.FieldNames)
            {
                var element = NodeFactory.CreateNode(_nodeData.ChildData(item));
                _inspectorElements.Add(element);
                _field.Add(element);
            }
            Content = _field;
        }

        public override void UpdateData(EntityReference entity)
        {
            foreach (var inspectorElement in _inspectorElements)
                inspectorElement.UpdateData(entity);
        }
    }
}
