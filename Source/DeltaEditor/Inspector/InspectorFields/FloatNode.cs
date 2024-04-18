using Arch.Core;

namespace DeltaEditor.Inspector.InspectorFields
{
    internal class FloatNode : FieldNode<float>
    {
        public FloatNode(NodeData parameters, bool withName = true) : base(parameters, withName)
        {

        }

        public override void UpdateData(EntityReference entity)
        {
            if (!_fieldData.IsFocused)
                _fieldData.Text = GetData(entity).ToString("0.00");
            else
                TrySetValue(entity);
        }

        private void TrySetValue(EntityReference entity)
        {
            if (string.IsNullOrEmpty(_fieldData.Text))
                SetData(entity, default);
            else if (float.TryParse(_fieldData.Text, out float result))
                SetData(entity, result);
        }

    }
}
