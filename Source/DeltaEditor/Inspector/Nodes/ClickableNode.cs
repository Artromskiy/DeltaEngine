
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor.Inspector.Nodes;

internal abstract class ClickableNode<T> : Node<T>
{
    private readonly HorizontalStackLayout _stack;
    protected readonly Button _fieldData = new()
    {
        MaximumHeightRequest = NodeHeight,
        MinimumHeightRequest = NodeHeight,
        Margin = 0,
        Padding = 0
    };

    public ClickableNode(NodeData parameters, bool withName) : base(parameters)
    {
        if (withName)
            _stack = [_fieldName, _fieldData];
        else
            _stack = [_fieldData];
        _stack.BackgroundColor = NodeConst.BackColor;
        _stack.VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true);
        _stack.HorizontalOptions = new LayoutOptions(LayoutAlignment.Start, true);
        ValueMode = FieldSizeMode.Default;
        Content = _stack;
    }

    public FieldSizeMode ValueMode
    {
        set => _fieldData.MaximumWidthRequest = _fieldData.MinimumWidthRequest = SizeModeToSize(value);
    }

}
