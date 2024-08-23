using Arch.Core;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor.Inspector.Nodes;


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

    public override bool UpdateData(EntityReference entity)
    {
        bool changed = false;
        foreach (var inspectorElement in _inspectorElements)
            changed|=inspectorElement.UpdateData(entity);
        return changed;
    }
}
