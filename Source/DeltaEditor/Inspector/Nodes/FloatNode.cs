using Arch.Core;
using DeltaEditor.Inspector.Internal;
using DeltaEditor.Tools;

namespace DeltaEditor.Inspector.Nodes;


internal class FloatNode : FieldNode<float>
{
    private const string FloatFormat = "0.00";
    public FloatNode(NodeData parameters, bool withName = true) : base(parameters, withName) { }

    public override bool UpdateData(EntityReference entity)
    {
        if (!_fieldData.IsFocused)
            _fieldData.Text = GetData(entity).LookupString();
        else
        {
            if (string.IsNullOrEmpty(_fieldData.Text))
                SetData(entity, default);
            else if (float.TryParse(_fieldData.Text, out float result))
                SetData(entity, result);
            return true;
        }
        return false;
    }
}