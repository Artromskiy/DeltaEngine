using Arch.Core;
using Arch.Core.Extensions;
using DeltaEditor.Inspector;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace DeltaEditor
{
    internal class InspectorView : ContentView
    {
        private readonly List<IInspectorElement> _componentEditors = [];
        private readonly VerticalStackLayout _inspectorStack;
        public readonly ObservableCollection<InspectorAvaliableComponent> AvaliableComponents = [];

        private readonly IAccessorsContainer _accessorsContainer;
        private readonly ImmutableArray<Type> _components;

        private EntityReference SelectedEntity = EntityReference.Null;
        private Archetype? CurrentArch;

        private readonly Dictionary<Type, IInspectorElement> _inspectors = [];
        public InspectorView(IAccessorsContainer accessorsContainer, IEnumerable<Type> components)
        {
            _accessorsContainer = accessorsContainer;
            _components = components.ToImmutableArray();
            _inspectorStack = [];
            Content = _inspectorStack;
        }

        public void UpdateComponentsEntity(EntityReference entityReference)
        {
            SelectedEntity = entityReference;

            _inspectorStack.Clear();
            _componentEditors.Clear();
            AvaliableComponents.Clear();

            if (!SelectedEntity.IsAlive())
            {
                CurrentArch = null;
                return;
            }
            CurrentArch = SelectedEntity.Entity.GetArchetype();

            foreach (var type in SelectedEntity.Entity.GetComponentTypes())
            {
                if (!_accessorsContainer.AllAccessors.ContainsKey(type))
                    continue;
                var componentEditor = GetOrCreateInspector(type);
                _inspectorStack.Add(componentEditor);
                _componentEditors.Add(componentEditor);
            }
            UpdateComponentsData();
            UpdateComponentAdder();
        }

        public void UpdateComponentsData()
        {
            if (!SelectedEntity.IsAlive() || SelectedEntity.Entity.GetArchetype() != CurrentArch)
            {
                UpdateComponentsEntity(EntityReference.Null);
                return;
            }
            foreach (var item in _componentEditors)
                item.UpdateData(SelectedEntity);
        }

        private void UpdateComponentAdder()
        {
            foreach (var item in _components)
                if (!Array.Exists(SelectedEntity.Entity.GetComponentTypes(), c => c.Type.Equals(c)))
                    AvaliableComponents.Add(new(item.Name));
        }

        private IInspectorElement GetOrCreateInspector(Type type)
        {
            if (!_inspectors.TryGetValue(type, out var inspector))
                _inspectors[type] = inspector = InspectorElementFactory.CreateComponentInspector(new(type, _accessorsContainer));
            return inspector;
        }


    }
}
