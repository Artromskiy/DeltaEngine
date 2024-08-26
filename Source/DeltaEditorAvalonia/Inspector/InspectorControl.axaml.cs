using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Avalonia.Controls;
using Delta.Runtime;
using Delta.Scripting;
using Delta.Utilities;
using DeltaEditorAvalonia.Inspector.Internal;
using DeltaEditorLib.Loader;
using DeltaEditorLib.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DeltaEditorAvalonia;

public partial class InspectorControl : UserControl
{
    private readonly Dictionary<Type, INode> _currentComponentInspectors = [];
    private readonly Dictionary<Type, INode> _loadedComponentInspectors = [];

    private EntityReference SelectedEntity = EntityReference.Null;
    private Archetype? CurrentArch;

    private readonly IAccessorsContainer _accessors;
    private readonly ImmutableArray<Type> _components;

    public InspectorControl()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
            return;

        _accessors = Program.RuntimeLoader.Accessors;
        _components = [.. Program.RuntimeLoader.Components];
    }

    public void SetSelectedEntity(EntityReference entityReference)
    {
        SelectedEntity = entityReference;
    }

    public void UpdateInspector(IRuntime runtime)
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
            //RebuildComponentAdder();
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
            InspectorStack.Children.Add((Control)componentEditor);
            _currentComponentInspectors.Add(type, componentEditor);
        }
        //InspectorStack.Children.Add(_addComponentPicker);
    }

    private void ClearHandledEntityData()
    {
        SelectedEntity = EntityReference.Null;
        CurrentArch = null;
    }

    private void ClearInspector()
    {
        if (InspectorStack.Children.Count != 0)
            InspectorStack.Children.Clear();
        _currentComponentInspectors.Clear();
        //if (_addComponentList.Count != 0)
        //    _addComponentList.Clear();
    }

    private INode GetOrCreateInspector(IRuntime _, Type type)
    {
        //if (!_loadedComponentInspectors.TryGetValue(type, out var inspector))
        //    _loadedComponentInspectors[type] = inspector = NodeFactory.CreateComponentInspector(new(new(type, _runtimeLoader), new([])));
        //return inspector;
        return null;
    }
}