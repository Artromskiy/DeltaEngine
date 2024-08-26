using Arch.Core;
using DeltaEditor.Inspector.Internal;
using System.Numerics;

namespace DeltaEditor.Inspector.Nodes;


internal class Vector3Node : Node<Vector3>
{
    private readonly HorizontalStackLayout _stack;

    private readonly List<INode> _inspectorElements;

    public Vector3Node(NodeData parameters) : base(parameters)
    {
        _stack = [_fieldName];
        _stack.BackgroundColor = NodeConst.BackColor;
        _stack.Spacing = NodeConst.Spacing;
        _inspectorElements = [];
        foreach (var item in _nodeData.FieldNames)
        {
            var element = new FloatNode(_nodeData.ChildData(item)) { NameMode = FieldSizeMode.ExtraSmall };
            _inspectorElements.Add(element);
            _stack.Add(element);
        }
        Content = _stack;
    }

    public override bool UpdateData(EntityReference entity)
    {
        bool changed = false;
        foreach (var inspectorElement in _inspectorElements)
            changed |= inspectorElement.UpdateData(entity);
        return changed;
    }
}
