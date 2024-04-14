using Arch.Core;

namespace DeltaEditor.Inspector
{
    internal class ComponentInspectorElement : ContentView, IInspectorElement
    {
        private readonly Label _componentName;
        private readonly StackLayout _field;

        private readonly List<IInspectorElement> _inspectorElements;

        public ComponentInspectorElement(InspectorElementParam parameters)
        {
            HashSet<Type> visited = [parameters.ComponentType];
            _componentName = new() { Text = parameters.ComponentType.Name };
            _field = [_componentName];
            _inspectorElements = [];
            var fieldType = parameters.ComponentType;
            var accessor = parameters.AccessorsContainer.AllAccessors[fieldType];
            foreach (var item in accessor.FieldNames)
            {
                var element = InspectorElementFactory.CreateInspectorElement(parameters, visited, [item]);
                _inspectorElements.Add(element);
                _field.Add(element);
            }
            Content = _field;
        }

        public void UpdateData(EntityReference entity)
        {
            foreach (var inspectorElement in _inspectorElements)
                inspectorElement.UpdateData(entity);
        }
    }
}
