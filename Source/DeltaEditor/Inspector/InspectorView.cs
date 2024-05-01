using Arch.Core;
using Arch.Core.Extensions;
using Delta.Runtime;
using Delta.Scripting;
using DeltaEditor.Inspector.Internal;
using DeltaEditorLib.Loader;
using DeltaEditorLib.Scripting;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace DeltaEditor.Inspector;

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

    public void UpdateComponentsEntity(EntityReference entityReference)
    {
        SelectedEntity = entityReference;
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
        var types = SelectedEntity.Entity.GetComponentTypes().ToArray();
        Array.Sort(types, (x1, x2) =>
        {
            var attr1 = x1.Type.GetAttribute<ComponentAttribute>();
            var attr2 = x2.Type.GetAttribute<ComponentAttribute>();
            return ComponentAttribute.Compare(attr2, attr1);
        });
        foreach (var type in types)
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
        if (_inspectorStack.Count != 0)
            _inspectorStack.Clear();
        _componentEditors.Clear();
        if (_componentsToAdd.Count != 0)
            _componentsToAdd.Clear();
    }

    private void RebuildComponentAdder()
    {
        _componentsToAdd.Add(new InspectorAvaliableComponent(string.Empty, null!));
        foreach (var item in _components)
            if (!Array.Exists(SelectedEntity.Entity.GetComponentTypes(), c => c.Type.Equals(item)))
                _componentsToAdd.Add(new(item.Name, item));
    }

    private INode GetOrCreateInspector(IRuntime _, Type type)
    {
        if (!_inspectors.TryGetValue(type, out var inspector))
            _inspectors[type] = inspector = NodeFactory.CreateComponentInspector(new(new(type, _runtimeLoader), new([])));
        return inspector;
    }

    private void AddComponentClicked(object? sender, EventArgs eventArgs)
    {
        if (_addComponentPicker.SelectedIndex == 0 || _addComponentPicker.SelectedItem is not InspectorAvaliableComponent componentType)
            return;
        var type = componentType.Type;

        _addComponentPicker.SelectedIndex = 0;

        _runtimeLoader.OnRuntimeThread += (r) =>
        {
            if (!SelectedEntity.Entity.IsAlive())
                return;

            object? component = Activator.CreateInstance(type);
            if (component == null)
                return;

            SelectedEntity.Entity.Add(component);
        };
    }
}