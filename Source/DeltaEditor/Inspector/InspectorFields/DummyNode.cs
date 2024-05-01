using Arch.Core;

namespace DeltaEditor.Inspector.InspectorFields;

internal class DummyNode<T> : FieldNode<T>
{
    public string Value
    {
        get => _fieldData.Text;
        set => _fieldData.Text = value;
    }
    public bool FocusedField => _fieldData.IsFocused;
    protected sealed override bool SuppressTypeCheck => true;
    public DummyNode(NodeData parameters, bool withName = true) : base(parameters, withName)
    {

    }

    public override void UpdateData(EntityReference entity)
    {

    }
}
