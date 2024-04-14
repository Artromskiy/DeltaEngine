using Arch.Core;
using DeltaEditorLib.Scripting;

namespace DeltaEditor.Inspector
{
    internal class EditorField : ContentView, IInspectorElement
    {
        private readonly InspectorElementParam _parameters;
        private readonly List<string> _path;
        private readonly Type _fieldType;

        private readonly HorizontalStackLayout _stack;
        private readonly Label _fieldName;
        private readonly Entry _fieldData;

        private readonly Dictionary<Type, Func<EditorField, EntityReference, string>> _converters = new()
        {
            { typeof(float), GetFieldValue<float> },
            { typeof(int), GetFieldValue<int> },
            { typeof(string), GetFieldValue<string> },
        };

        public EditorField(InspectorElementParam parameters, List<string> path)
        {
            _parameters = parameters;
            _path = path;
            _fieldType = _parameters.AccessorsContainer.GetFieldType(_parameters.ComponentType, _path);
            _fieldData = new() { Text = "", VerticalTextAlignment = TextAlignment.Center };
            _fieldName = new() { Text = _path[^1], VerticalTextAlignment = TextAlignment.Center };
            _stack = [_fieldName, _fieldData];
            _stack.VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true);
            _stack.HorizontalOptions = new LayoutOptions(LayoutAlignment.Start, true);
            Content = _stack;
        }

        public void UpdateData(EntityReference entity) => _fieldData.Text = _converters[_fieldType].Invoke(this, entity);

        private static string GetFieldValue<T>(EditorField f, EntityReference entity)
        {
            var container = f._parameters.AccessorsContainer;
            return container.GetComponentFieldValue<T>(entity, f._parameters.ComponentType, f._path).ToString();
        }
    }
}