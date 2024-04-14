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
            { typeof(float), GetFieldValueFloat },
            { typeof(int), GetFieldValue<int> },
            { typeof(string), GetFieldValue<string> },
        };

        public EditorField(InspectorElementParam parameters, List<string> path, bool withName = true)
        {
            _parameters = parameters;
            _path = path;
            _fieldType = _parameters.AccessorsContainer.GetFieldType(_parameters.ComponentType, _path);
            _fieldData = new() { Text = "", VerticalTextAlignment = TextAlignment.Center };
            _fieldName = new() { Text = _path[^1], VerticalTextAlignment = TextAlignment.Center };
            if (withName)
                _stack = [_fieldName, _fieldData];
            else
                _stack = [_fieldData];
            _stack.VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true);
            _stack.HorizontalOptions = new LayoutOptions(LayoutAlignment.Start, true);
            _stack.Padding = new Thickness(3, 0, 3, 0);
            _stack.Margin = new Thickness(3, 0, 3, 0);
            Content = _stack;
        }

        public void UpdateData(EntityReference entity)
        {
            if (!_fieldData.IsFocused)
                _fieldData.Text = _converters[_fieldType].Invoke(this, entity);
            else
                TrySetValue(entity);
        }

        private void TrySetValue(EntityReference entity)
        {
            if (_fieldType == typeof(string))
                SetFieldValueString(entity);
            else if (_fieldType == typeof(float))
                SetFieldValueFloat(entity);
            else if (_fieldType == typeof(int))
                SetFieldValueInt(entity);
        }

        private static string GetFieldValue<T>(EditorField f, EntityReference entity)
        {
            var container = f._parameters.AccessorsContainer;
            return container.GetComponentFieldValue<T>(entity, f._parameters.ComponentType, f._path).ToString();
        }
        private static string GetFieldValueFloat(EditorField f, EntityReference entity)
        {
            var container = f._parameters.AccessorsContainer;
            return container.GetComponentFieldValue<float>(entity, f._parameters.ComponentType, f._path).ToString("0.00");
        }
        private void SetFieldValueFloat(EntityReference entity)
        {
            var text = _fieldData.Text;
            if(float.TryParse(text, out float result))
            {
                var container = _parameters.AccessorsContainer;
                container.SetComponentFieldValue(entity, _parameters.ComponentType, _path, result);
            }
        }
        private void SetFieldValueString(EntityReference entity)
        {
            var container = _parameters.AccessorsContainer;
            container.SetComponentFieldValue(entity, _parameters.ComponentType, _path, _fieldData.Text);
        }
        private void SetFieldValueInt(EntityReference entity)
        {
            var text = _fieldData.Text;
            if (int.TryParse(text, out int result))
            {
                var container = _parameters.AccessorsContainer;
                container.SetComponentFieldValue(entity, _parameters.ComponentType, _path, result);
            }
        }
    }
}