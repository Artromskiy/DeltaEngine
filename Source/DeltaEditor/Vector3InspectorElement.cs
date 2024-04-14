using Arch.Core;
using Arch.Core.Utils;
using DeltaEditor.Inspector;
using DeltaEditorLib.Scripting;
using System.ComponentModel;
using System.Numerics;

namespace DeltaEditor
{
    internal class Vector3InspectorElement : ContentView, IInspectorElement
    {
        private readonly Label _fieldName;
        private readonly HorizontalStackLayout _field;
        
        private readonly List<IInspectorElement> _inspectorElements;

        public Vector3InspectorElement(InspectorElementParam parameters, HashSet<Type> visited, List<string> path)
        {
            var type = parameters.AccessorsContainer.GetFieldType(parameters.ComponentType, path);
            if (type != typeof(Vector3))
                throw new InvalidOperationException("not Vector3");

            _fieldName = new() { Text = path[^1] };
            _field = [_fieldName];
            _inspectorElements = [];
            var fieldType = parameters.AccessorsContainer.GetFieldType(parameters.ComponentType, path);
            var accessor = parameters.AccessorsContainer.AllAccessors[fieldType];
            foreach (var item in accessor.FieldNames)
            {
                var element = InspectorElementFactory.CreateInspectorElement(parameters, visited, new(path) { item });
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
