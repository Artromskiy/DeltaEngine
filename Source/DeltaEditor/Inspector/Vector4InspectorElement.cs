using Arch.Core;
using DeltaEditorLib.Scripting;
using System.Numerics;

namespace DeltaEditor.Inspector
{
    internal class Vector4InspectorElement : ContentView, IInspectorElement
    {
        private readonly Label _fieldName;
        private readonly HorizontalStackLayout _field;

        private readonly List<IInspectorElement> _inspectorElements;

        public Vector4InspectorElement(InspectorElementParam parameters, HashSet<Type> visited, List<string> path)
        {
            var type = parameters.AccessorsContainer.GetFieldType(parameters.ComponentType, path);
            if (type != typeof(Vector4))
                throw new InvalidOperationException($"Type of field is not{nameof(Vector4)} in path {string.Join(",", path)}");

            _fieldName = new() { Text = path[^1], VerticalTextAlignment = TextAlignment.Center };
            _field = [_fieldName];
            _inspectorElements = [];
            var fieldType = parameters.AccessorsContainer.GetFieldType(parameters.ComponentType, path);
            var accessor = parameters.AccessorsContainer.AllAccessors[fieldType];
            foreach (var item in accessor.FieldNames)
            {
                var element = new EditorField(parameters, new(path) { item });
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
