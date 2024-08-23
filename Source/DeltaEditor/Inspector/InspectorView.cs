using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Delta.Runtime;
using Delta.Scripting;
using Delta.Utilities;
using DeltaEditor.Inspector.Internal;
using DeltaEditorLib.Loader;
using DeltaEditorLib.Scripting;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace DeltaEditor.Inspector;

internal class InspectorView : ContentView
{
    private readonly Dictionary<Type, INode> _currentComponentInspectors = [];
    private readonly ObservableCollection<InspectorAvaliableComponent> _addComponentList = [];
    private readonly VerticalStackLayout _inspectorStack;
    private readonly Picker _addComponentPicker;

    private readonly RuntimeLoader _runtimeLoader;

    private readonly IAccessorsContainer _accessors;
    private readonly ImmutableArray<Type> _components;

    private EntityReference SelectedEntity = EntityReference.Null;
    private Archetype? CurrentArch;

    private readonly Dictionary<Type, INode> _loadedComponentInspectors = [];
    public InspectorView(RuntimeLoader runtimeLoader)
    {
        _runtimeLoader = runtimeLoader;
        _accessors = _runtimeLoader.Accessors;
        _components = [.. _runtimeLoader.Components];
        _inspectorStack = [];
        _addComponentPicker = new()
        {
            ItemsSource = _addComponentList,
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
        foreach (var item in _currentComponentInspectors)
        {
            bool changed = item.Value.UpdateData(SelectedEntity);
            if (changed && item.Key.HasAttribute<DirtyAttribute>())
            {
                // TODO mark dirty
            }
        }
    }

    private void RebuildInspectorComponents(IRuntime runtime)
    {
        var types = SelectedEntity.Entity.GetComponentTypes();
        Span<ComponentType> typesSpan = stackalloc ComponentType[types.Length];
        types.CopyTo(typesSpan);
        typesSpan.Sort(NullSafeAttributeComparer<ComponentAttribute>.Default);
        foreach (var type in typesSpan)
        {
            if (!_accessors.AllAccessors.ContainsKey(type))
                continue;
            var componentEditor = GetOrCreateInspector(runtime, type);
            _inspectorStack.Add(componentEditor);
            _currentComponentInspectors.Add(type, componentEditor);
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
        _currentComponentInspectors.Clear();
        if (_addComponentList.Count != 0)
            _addComponentList.Clear();
    }

    private void RebuildComponentAdder()
    {
        _addComponentList.Add(new InspectorAvaliableComponent(string.Empty, null!));
        foreach (var item in _components)
            if (!Array.Exists(SelectedEntity.Entity.GetComponentTypes(), c => c.Type.Equals(item)))
                _addComponentList.Add(new(item.Name, item));
    }

    private INode GetOrCreateInspector(IRuntime _, Type type)
    {
        if (!_loadedComponentInspectors.TryGetValue(type, out var inspector))
            _loadedComponentInspectors[type] = inspector = NodeFactory.CreateComponentInspector(new(new(type, _runtimeLoader), new([])));
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