using Arch.Core;
using DeltaEditorLib.Scripting;

namespace DeltaEditor.Inspector
{
    internal class DefaultInspectorElement : ContentView, IInspectorElement
    {
        private readonly Label _fieldName;
        //private readonly HorizontalStackLayout _field;
        private readonly StackLayout _elements;

        private readonly List<IInspectorElement> _inspectorElements;

        public DefaultInspectorElement(InspectorElementParam parameters, HashSet<Type> visited, List<string> path)
        {
            _fieldName = new() { Text = path[^1] };
            //_field = [_fieldName];
            _elements = [_fieldName];
            _elements.Padding = new Thickness(3);
            _inspectorElements = [];
            var fieldType = parameters.AccessorsContainer.GetFieldType(parameters.ComponentType, path);
            var accessor = parameters.AccessorsContainer.AllAccessors[fieldType];
            foreach (var item in accessor.FieldNames)
            {
                var element = InspectorElementFactory.CreateInspectorElement(parameters, visited, new(path) { item });
                _inspectorElements.Add(element);
                _elements.Add(element);
            }
            Content = _elements;
        }

        public void UpdateData(EntityReference entity)
        {
            foreach (var inspectorElement in _inspectorElements)
                inspectorElement.UpdateData(entity);
        }
    }
}
