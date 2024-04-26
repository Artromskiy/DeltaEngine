using Arch.Core;
using System.Numerics;

namespace DeltaEditor.Inspector.InspectorElements
{
    internal class Matrix4Node : Node<Matrix4x4>
    {
        private readonly HorizontalStackLayout _field;
        private readonly Grid _grid;

        private readonly List<INode> _inspectorElements;

        public Matrix4Node(NodeData parameters) : base(parameters)
        {
            _grid = new()
            {
                BackgroundColor = NodeConst.BackColor,
                ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)],
                RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)]
            };

            _field = [_fieldName];
            _inspectorElements = [];
            int index = 0;
            foreach (var item in _nodeData.FieldNames)
            {
                var element = new InspectorFields.FloatNode(_nodeData.ChildData(item), false) { ValueMode = FieldSizeMode.Small };
                _grid.Add(element, index % 4, index / 4);
                _inspectorElements.Add(element);

                index++;
            }

            _field.Add(_grid);
            Content = _field;
        }

        public override void UpdateData(EntityReference entity)
        {
            foreach (var inspectorElement in _inspectorElements)
                inspectorElement.UpdateData(entity);
        }
    }
}