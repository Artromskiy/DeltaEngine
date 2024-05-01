using DeltaEditor.Inspector.Internal;

namespace DeltaEditor.Inspector.Nodes;


internal abstract class FieldNode<T> : Node<T>
{
    private readonly HorizontalStackLayout _stack;
    protected readonly Entry _fieldData = new()
    {
        VerticalTextAlignment = TextAlignment.Center,
        MaximumHeightRequest = NodeHeight,
        MinimumHeightRequest = NodeHeight,
        BackgroundColor = NodeConst.BackColor,
    };

    public FieldNode(NodeData parameters, bool withName) : base(parameters)
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
