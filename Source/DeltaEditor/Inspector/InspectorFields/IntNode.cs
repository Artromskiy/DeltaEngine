using Arch.Core;

namespace DeltaEditor.Inspector.InspectorFields;

internal class IntNode : FieldNode<int>
{
    public IntNode(NodeData parameters, bool withName = true) : base(parameters, withName)
    {

    }

    public override void UpdateData(EntityReference entity)
    {
        if (!_fieldData.IsFocused)
            _fieldData.Text = GetData(entity).ToString();
        else
            TrySetValue(entity);
    }

    private void TrySetValue(EntityReference entity)
    {
        if (string.IsNullOrEmpty(_fieldData.Text))
            SetData(entity, default);
        else if (int.TryParse(_fieldData.Text, out int result))
            SetData(entity, result);
    }

}
