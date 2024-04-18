using Arch.Core;
using DeltaEditor.Inspector.InspectorFields;
using System.Numerics;

namespace DeltaEditor.Inspector
{
    internal class Vector4Node : Node<Vector4>
    {
        private readonly HorizontalStackLayout _field;

        private readonly List<INode> _inspectorElements;

        public Vector4Node(NodeData parameters) : base(parameters)
        {
            _field = [_fieldName];
            _inspectorElements = [];
            foreach (var item in _nodeData.FieldNames)
            {
                var element = new FloatNode(_nodeData.ChildData(item)) { NameMode = FieldSizeMode.Small };
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
