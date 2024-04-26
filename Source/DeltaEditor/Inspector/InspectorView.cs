using Arch.Core;
using Arch.Core.Extensions;
using Delta.Runtime;
using DeltaEditorLib.Scripting;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace DeltaEditor.Inspector
{
    internal class InspectorView : ContentView
    {
        private readonly List<INode> _componentEditors = [];
        private readonly ObservableCollection<InspectorAvaliableComponent> _componentsToAdd = [];
        private readonly VerticalStackLayout _inspectorStack;
        private readonly Picker _addComponentPicker;

        private readonly RuntimeLoader _runtimeLoader;

        private readonly IAccessorsContainer _accessors;
        private readonly ImmutableArray<Type> _components;

        private EntityReference SelectedEntity = EntityReference.Null;
        private Archetype? CurrentArch;

        private readonly Dictionary<Type, INode> _inspectors = [];
        public InspectorView(RuntimeLoader runtimeLoader)
        {
            _runtimeLoader = runtimeLoader;
            _accessors = _runtimeLoader.Accessors;
            _components = [.. _runtimeLoader.Components];
            _inspectorStack = [];
            _addComponentPicker = new()
            {
                ItemsSource = _componentsToAdd,
                Title = "Add Component",
                HorizontalOptions = new LayoutOptions(LayoutAlignment.Center, false),
            };
            _addComponentPicker.SelectedIndexChanged += AddComponentClicked;
            _addComponentPicker.ItemDisplayBinding = new Binding("Name");
            _inspectorStack.BackgroundColor = NodeConst.BackColor;
            _inspectorStack.Add(_addComponentPicker);
            BackgroundColor = NodeConst.BackColor;
            Content = _inspectorStack;
        }

        public void UpdateComponentsEntity(IRuntime runtime, EntityReference entityReference)
        {
            SelectedEntity = entityReference;
            UpdateComponentsData(runtime);
        }

        public void UpdateComponentsData(IRuntime runtime)
        {
            if (!SelectedEntity.IsAlive()) // Dead entity
            {
                ClearHandledEntityData();
                ClearInspector();
                return;
            }
            if (CurrentArch != SelectedEntity.Entity.GetArchetype()) // Arch changed
            {
                CurrentArch = SelectedEntity.Entity.GetArchetype();
                ClearInspector();
                RebuildInspectorComponents(runtime);
                RebuildComponentAdder();
            }
            foreach (var item in _componentEditors)
                item.UpdateData(SelectedEntity);
        }

        private void RebuildInspectorComponents(IRuntime runtime)
        {
            foreach (var type in SelectedEntity.Entity.GetComponentTypes())
            {
                if (!_accessors.AllAccessors.ContainsKey(type))
                    continue;
                var componentEditor = GetOrCreateInspector(runtime, type);
                _inspectorStack.Add(componentEditor);
                _componentEditors.Add(componentEditor);
            }
            _inspectorStack.Add(_addComponentPicker);
        }

        private void ClearHandledEntityData()
        {
            SelectedEntity = EntityReference.Null;
            CurrentArch = null;
        }

        private void ClearInspector()
        {
            _inspectorStack.Clear();
            _componentEditors.Clear();
            _componentsToAdd.Clear();
        }

        private void RebuildComponentAdder()
        {
            foreach (var item in _components)
                if (!Array.Exists(SelectedEntity.Entity.GetComponentTypes(), c => c.Type.Equals(item)))
                    _componentsToAdd.Add(new(item.Name, item));
        }

        private INode GetOrCreateInspector(IRuntime runtime, Type type)
        {
            if (!_inspectors.TryGetValue(type, out var inspector))
                _inspectors[type] = inspector = NodeFactory.CreateComponentInspector(new(new(type, _accessors, runtime.Context), new([])));
            return inspector;
        }

        private void AddComponentClicked(object? sender, EventArgs eventArgs)
        {
            if (_addComponentPicker.SelectedItem is not InspectorAvaliableComponent componentType)
                return;

            var type = componentType.Type;

            _runtimeLoader.OnRuntimeThread += (r) =>
            {
                if (SelectedEntity.Entity.IsAlive())
                    return;

                object? component = Activator.CreateInstance(type);
                if (component == null)
                    return;

                SelectedEntity.Entity.Add(component);
            };
        }
    }
}
