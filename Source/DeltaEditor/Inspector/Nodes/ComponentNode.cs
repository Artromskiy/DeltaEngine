using Arch.Core;
using Arch.Core.Extensions;
using Arch.Core.Utils;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor.Inspector.Nodes;

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
    private readonly Button _removeButton = new()
    {
        MaximumWidthRequest = NodeConst.NodeHeight,
        MinimumWidthRequest = NodeConst.NodeHeight,
        MaximumHeightRequest = NodeConst.NodeHeight,
        MinimumHeightRequest = NodeConst.NodeHeight,
        Margin = 0,
        Padding = 0,
        Text = "X",
    };

    private readonly StackLayout _fieldHeader;
    private readonly StackLayout _fieldContent;
    private readonly Grid _grid = new()
    {
        BackgroundColor = NodeConst.BackColor,
        ColumnDefinitions = [new(GridLength.Auto)],
        RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto)]
    };
    protected override bool SuppressTypeCheck => true;

    private readonly List<INode> _inspectorElements = [];

    private EntityReference _cachedEntity;

    public ComponentNode(NodeData parameters) : base(parameters)
    {
        _removeButton.Clicked += Remove;
        _componentName.Text = _nodeData.FieldName;
        _fieldHeader = [_componentName, _removeButton];
        _fieldHeader.Spacing = 40;
        _fieldHeader.Orientation = StackOrientation.Horizontal;
        _fieldHeader.HorizontalOptions = new LayoutOptions(LayoutAlignment.Center, true);
        _fieldContent = [];
        _grid.Add(_fieldHeader, 0, 0);
        _grid.Add(_fieldContent, 0, 1);
        _fieldHeader.BackgroundColor = NodeConst.BackColor;
        _fieldContent.Background = NodeConst.BackColor;
        foreach (var item in _nodeData.FieldNames)
        {
            var element = NodeFactory.CreateNode(_nodeData.ChildData(item));
            _inspectorElements.Add(element);
            _fieldContent.Add(element);
        }
        _border.Content = _grid;
        Content = _border;
    }

    public override bool UpdateData(EntityReference entity)
    {
        _cachedEntity = entity;
        bool changed = false;
        foreach (var inspectorElement in _inspectorElements)
            changed |= inspectorElement.UpdateData(_cachedEntity);
        return changed;
    }

    public void Remove(object? sender, EventArgs eventArgs)
    {
        _nodeData.rootData.RuntimeLoader.OnRuntimeThread += RemoveComponent;
    }

    private void RemoveComponent(IRuntimeContext _)
    {
        _cachedEntity.Entity.RemoveRange(Component.GetComponentType(_nodeData.Component));
    }
}
