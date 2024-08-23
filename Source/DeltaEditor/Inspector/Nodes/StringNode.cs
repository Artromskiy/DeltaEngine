using Arch.Core;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor.Inspector.Nodes;


internal class StringNode : FieldNode<string>
{
    public StringNode(NodeData parameters, bool withName = true) : base(parameters, withName) { }

    public override bool UpdateData(EntityReference entity)
    {
        bool changed = _fieldData.IsFocused;
        if (!changed)
            _fieldData.Text = GetData(entity);
        else
            SetData(entity, _fieldData.Text);
        return changed;
    }

}