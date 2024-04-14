using Arch.Core;
using Arch.Core.Extensions;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace DeltaEditor.Inspector
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
            UpdateComponentsData();
        }

        public void UpdateComponentsData()
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
                RebuildInspectorComponents();
                RebuildComponentAdder();
            }
            foreach (var item in _componentEditors)
                item.UpdateData(SelectedEntity);
        }

        private void RebuildInspectorComponents()
        {
            foreach (var type in SelectedEntity.Entity.GetComponentTypes())
            {
                if (!_accessorsContainer.AllAccessors.ContainsKey(type))
                    continue;
                var componentEditor = GetOrCreateInspector(type);
                _inspectorStack.Add(componentEditor);
                _componentEditors.Add(componentEditor);
            }
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
            AvaliableComponents.Clear();
        }

        private void RebuildComponentAdder()
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
