using Arch.Core;

namespace DeltaEditor.Inspector
{
    internal class EmptyInspectorElement : ContentView, IInspectorElement
    {
        private readonly Label _fieldName;
        public EmptyInspectorElement(string fieldName)
        {
            _fieldName = new() { Text = fieldName };
            Content = _fieldName;
        }
        public void UpdateData(EntityReference entity) { }
    }
}
