using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Avalonia.Controls;
using Delta.ECS;
using Delta.ECS.Attributes;
using Delta.Runtime;
using Delta.Utilities;
using DeltaEditor.Inspector.Internal;
using DeltaEditorLib.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DeltaEditor;

public partial class InspectorControl : UserControl
{
    private readonly Dictionary<Type, INode> _currentComponentInspectors = [];
    private readonly Dictionary<Type, INode> _loadedComponentInspectors = [];
    private readonly HashSet<Type> _notUsedComponentTypes = [];

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
        AddComponentControlFlyout.OnComponentAddRequested += OnComponentAddRequest;
    }

    public void SetSelectedEntity(EntityReference entityReference)
    {
        SelectedEntity = entityReference;
    }

    public void UpdateInspector(IRuntimeContext ctx)
    {
        PanelHeader.StartDebug();
        if (!SelectedEntity.IsAlive()) // Dead entity
        {
            ClearHandledEntityData();
            ClearInspector();
            AddComponentButton.IsVisible = false;
            AddComponentControlFlyout.UpdateComponents(_notUsedComponentTypes);
            PanelHeader.StopDebug();
            return;
        }
        AddComponentButton.IsVisible = true;
        if (CurrentArch != SelectedEntity.Entity.GetArchetype()) // Arch changed
        {
            CurrentArch = SelectedEntity.Entity.GetArchetype();
            ClearInspector();
            RebuildInspectorComponents(ctx);
            UpdateNotUsedComponentTypes();
            AddComponentControlFlyout.UpdateComponents(_notUsedComponentTypes);
        }
        foreach (var item in _currentComponentInspectors)
        {
            bool changed = item.Value.UpdateData(ref SelectedEntity);
            if (changed)
                SelectedEntity.Entity.MarkDirty(item.Key);
        }
        PanelHeader.StopDebug();
    }


    private void RebuildInspectorComponents(IRuntimeContext ctx)
    {
        var types = SelectedEntity.Entity.GetComponentTypes();
        Span<ComponentType> typesSpan = stackalloc ComponentType[types.Length];
        types.CopyTo(typesSpan);
        typesSpan.Sort(NullSafeAttributeComparer<ComponentAttribute>.Default);
        foreach (var type in typesSpan)
        {
            if (!_accessors.AllAccessors.ContainsKey(type))
                continue;
            var componentEditor = GetOrCreateInspector(ctx, type);
            InspectorStack.Children.Add((Control)componentEditor);
            _currentComponentInspectors.Add(type, componentEditor);
        }
    }

    private void OnComponentAddRequest(Type type)
    {
        Program.RuntimeLoader.OnRuntimeThread += AddComponent;
        void AddComponent(IRuntimeContext ctx)
        {
            var instance = Activator.CreateInstance(type);
            SelectedEntity.Entity.Add(instance!);
        }
    }
    private void OnComponentRemoveRequest(Type type)
    {
        Program.RuntimeLoader.OnRuntimeThread += RemoveComponent;
        void RemoveComponent(IRuntimeContext ctx)
        {
            SelectedEntity.Entity.RemoveRange(type);
        }
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
    }

    private INode GetOrCreateInspector(IRuntimeContext _, Type type)
    {
        if (!_loadedComponentInspectors.TryGetValue(type, out var inspector))
        {
            var rootData = new RootData(type, _accessors);
            var nodeData = new NodeData(rootData);
            var componentInspector = new ComponentNodeControl(nodeData);
            componentInspector.OnComponentRemoveRequest += OnComponentRemoveRequest;
            _loadedComponentInspectors[type] = inspector = componentInspector;
        }
        return inspector;
    }

    private void UpdateNotUsedComponentTypes()
    {
        _notUsedComponentTypes.Clear();
        foreach (var type in _components)
            if (!_currentComponentInspectors.ContainsKey(type))
                _notUsedComponentTypes.Add(type);
    }
}