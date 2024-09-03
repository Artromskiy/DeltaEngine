using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Avalonia.Controls;
using Delta.Runtime;
using Delta.Scripting;
using Delta.Utilities;
using DeltaEditor.Inspector;
using DeltaEditor.Inspector.Internal;
using DeltaEditorLib.Scripting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace DeltaEditor;

public partial class InspectorControl : UserControl
{
    private readonly Dictionary<Type, INode> _currentComponentInspectors = [];
    private readonly Dictionary<Type, INode> _loadedComponentInspectors = [];

    private EntityReference SelectedEntity = EntityReference.Null;
    private Archetype? CurrentArch;

    private readonly IAccessorsContainer _accessors;
    private readonly ImmutableArray<Type> _components;

    private readonly Stopwatch sw = new();
    private int prevTime = 0;

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
    public void UpdateInspector(IRuntimeContext ctx)
    {
        sw.Restart();
        if (!SelectedEntity.IsAlive()) // Dead entity
        {
            ClearHandledEntityData();
            ClearInspector();
            StopDebug();
            return;
        }
        if (CurrentArch != SelectedEntity.Entity.GetArchetype()) // Arch changed
        {
            CurrentArch = SelectedEntity.Entity.GetArchetype();
            ClearInspector();
            RebuildInspectorComponents(ctx);
            //RebuildComponentAdder();
        }
        foreach (var item in _currentComponentInspectors)
        {
            bool changed = item.Value.UpdateData(ref SelectedEntity);
            if (changed && item.Key.HasAttribute<DirtyAttribute>())
            {
                // TODO mark dirty
            }
        }
        StopDebug();
    }

    private void StopDebug()
    {
        sw.Stop();
        prevTime = SmoothInt(prevTime, (int)sw.Elapsed.TotalMicroseconds, 50);
        DebugTimer.Content = $"{prevTime}us";
    }

    private static int SmoothInt(int value1, int value2, int smoothing)
    {
        return ((value1 * smoothing) + value2) / (smoothing + 1);
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

    private INode GetOrCreateInspector(IRuntimeContext ctx, Type type)
    {
        if (!_loadedComponentInspectors.TryGetValue(type, out var inspector))
        {
            var rootData = new RootData(type, _accessors);
            var nodeData = new NodeData(rootData);
            _loadedComponentInspectors[type] = inspector = NodeFactory.CreateComponentInspector(nodeData);
        }
        return inspector;
    }
}