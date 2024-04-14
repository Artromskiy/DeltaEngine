using Arch.Core;
using DeltaEditorLib.Scripting;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DeltaEditor.Inspector
{
    internal class Matrix4x4InspectorElement : ContentView, IInspectorElement
    {
        private readonly Label _fieldName;
        private readonly HorizontalStackLayout _field;
        private readonly Grid _grid;

        private readonly List<IInspectorElement> _inspectorElements;

        public Matrix4x4InspectorElement(InspectorElementParam parameters, HashSet<Type> visited, List<string> path)
        {
            var type = parameters.AccessorsContainer.GetFieldType(parameters.ComponentType, path);
            if (type != typeof(Matrix4x4))
                throw new InvalidOperationException($"Type of field is not{nameof(Matrix4x4)} in path {string.Join(",", path)}");

            _grid = new()
            {
                ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)],
                RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Auto)]
            };

            _fieldName = new() { Text = path[^1], VerticalTextAlignment = TextAlignment.Center };
            _field = [_fieldName];
            _inspectorElements = [];
            var fieldType = parameters.AccessorsContainer.GetFieldType(parameters.ComponentType, path);
            var accessor = parameters.AccessorsContainer.AllAccessors[fieldType];
            int index = 0;
            foreach (var item in accessor.FieldNames)
            {
                var element = new EditorField(parameters, new(path) { item }, false);
                _grid.Add(element, index % 4, index / 4);
                _inspectorElements.Add(element);
                
                index++;
            }

            _field.Add(_grid);
            Content = _field;
        }

        public void UpdateData(EntityReference entity)
        {
            foreach (var inspectorElement in _inspectorElements)
                inspectorElement.UpdateData(entity);
        }
    }
}