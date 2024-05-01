using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Delta.Runtime;

namespace DeltaEditor.Inspector;

internal class ComponentNode : Node
{
    private readonly Label _componentName = new()
    {
        VerticalTextAlignment = TextAlignment.Center,
        HorizontalTextAlignment = TextAlignment.Center,
        MinimumHeightRequest = NodeHeight,
        MaximumHeightRequest = NodeHeight,
        FontSize = NodeConst.ComponentHeaderTextSize,
        Background = NodeConst.BackColor,
    };
    private readonly Border _border = new()
    {
        Stroke = NodeConst.BorderColor,
        Margin = -1,
        Padding = 5
    };
    private readonly Button _removeButton = new();

    private readonly StackLayout _field;
    protected override bool SuppressTypeCheck => true;

    private readonly List<INode> _inspectorElements = [];

    private EntityReference _cachedEntity;

    public ComponentNode(NodeData parameters) : base(parameters)
    {
        _removeButton.Clicked += Remove;
        _componentName.Text = _nodeData.FieldName;
        _border.Content = _field = [_componentName, _removeButton];
        _field.BackgroundColor = NodeConst.BackColor;
        foreach (var item in _nodeData.FieldNames)
        {
            var element = NodeFactory.CreateNode(_nodeData.ChildData(item));
            _inspectorElements.Add(element);
            _field.Add(element);
        }
        Content = _border;
    }

    public override void UpdateData(EntityReference entity)
    {
        _cachedEntity = entity;
        foreach (var inspectorElement in _inspectorElements)
            inspectorElement.UpdateData(_cachedEntity);
    }

    public void Remove(object? sender, EventArgs eventArgs)
    {
        _nodeData.rootData.RuntimeLoader.OnRuntimeThread += RemoveComponent;
    }

    private void RemoveComponent(IRuntime runtime)
    {
        _cachedEntity.Entity.RemoveRange(Component.GetComponentType(_nodeData.Component));
    }
}
