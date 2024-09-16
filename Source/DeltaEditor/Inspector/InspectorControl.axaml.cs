using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Avalonia.Controls;
using Delta.ECS;
using Delta.ECS.Attributes;
using Delta.ECS.Components;
using Delta.Runtime;
using Delta.Utilities;
using DeltaEditor.Hierarchy;
using DeltaEditor.Inspector.Internal;
using DeltaEditorLib.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace DeltaEditor;

public partial class InspectorControl : UserControl
{
    private readonly Dictionary<Type, ComponentNodeControl> _loadedComponentInspectors = [];
    private readonly HashSet<Type> _notUsedComponentTypes = [];
    private IListWrapper<ComponentNodeControl, Control> ChildrenNodes => new(InspectorStack.Children);

    private EntityReference SelectedEntity = EntityReference.Null;
    private Archetype? CurrentArch;

    private readonly IAccessorsContainer? _accessors;
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
        this.Focus();
    }

    public void UpdateInspector(IRuntimeContext ctx)
    {
        PanelHeader.StartDebug();
        if (!SelectedEntity.IsAlive()) // Dead entity
        {
            ClearHandledEntityData();
            EntityNameTextBox.Text = string.Empty;
            ChildrenNodes.Clear();
            AddComponentButton.IsVisible = false;
            AddComponentControlFlyout.UpdateComponents(_notUsedComponentTypes);
            PanelHeader.StopDebug();
            return;
        }
        AddComponentButton.IsVisible = true;
        if (CurrentArch != SelectedEntity.Entity.GetArchetype()) // Arch changed
        {
            CurrentArch = SelectedEntity.Entity.GetArchetype();
            ChildrenNodes.Clear();
            RebuildInspectorComponents(ctx);
            AddComponentControlFlyout.UpdateComponents(_notUsedComponentTypes);
        }
        UpdateName();
        foreach (var item in ChildrenNodes)
            if (item.UpdateData(ref SelectedEntity))
                SelectedEntity.Entity.MarkDirty(item.ComponentType);

        PanelHeader.StopDebug();
    }

    private void UpdateName()
    {
        bool editing = EntityNameTextBox.IsFocused;

        var name = EntityNameTextBox.Text;
        if (editing)
        {
            if (!string.IsNullOrEmpty(name))
                SelectedEntity.Entity.AddOrGet<EntityName>().name = name;
            else if (SelectedEntity.Entity.Has<EntityName>())
                SelectedEntity.Entity.Remove<EntityName>();
        }
        else
            EntityNameTextBox.Text =
                SelectedEntity.Entity.Has<EntityName>() ?
                SelectedEntity.Entity.Get<EntityName>().name :
                string.Empty;
    }

    private void RebuildInspectorComponents(IRuntimeContext ctx)
    {
        Debug.Assert(_accessors != null);
        var a = AttributeCache.GetAttribute<ComponentAttribute, Camera>();
        var types = SelectedEntity.Entity.GetComponentTypes();
        Span<ComponentType> typesSpan = stackalloc ComponentType[types.Length];
        types.CopyTo(typesSpan);
        typesSpan.Sort(NullSafeComponentAttributeComparer<ComponentAttribute>.Default);
        _notUsedComponentTypes.Clear();
        _notUsedComponentTypes.UnionWith(_components);
        foreach (var type in typesSpan)
        {
            _notUsedComponentTypes.Remove(type);
            if (!_accessors.AllAccessors.ContainsKey(type) || typeof(EntityName) == type.Type)
                continue;
            var componentInspector = GetOrCreateInspector(ctx, type);
            ChildrenNodes.Add(componentInspector);
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
    private ComponentNodeControl GetOrCreateInspector(IRuntimeContext _, Type type)
    {
        if (!_loadedComponentInspectors.TryGetValue(type, out var inspector))
        {
            Debug.Assert(_accessors != null);
            var rootData = new RootData(type, _accessors);
            var nodeData = new NodeData(rootData);
            var componentInspector = new ComponentNodeControl(nodeData);
            componentInspector.OnComponentRemoveRequest += OnComponentRemoveRequest;
            _loadedComponentInspectors[type] = inspector = componentInspector;
        }
        return inspector;
    }
}