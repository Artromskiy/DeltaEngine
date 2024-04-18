namespace DeltaEditor.Inspector.InspectorFields;

internal abstract class FieldNode<T> : Node<T>
{
    private readonly HorizontalStackLayout _stack;
    protected readonly Entry _fieldData = new()
    {
        VerticalTextAlignment = TextAlignment.Center,
        MaximumHeightRequest = NodeHeight,
        MinimumHeightRequest = NodeHeight
    };

    public FieldNode(NodeData parameters, bool withName) : base(parameters)
    {
        if (withName)
            _stack = [_fieldName, _fieldData];
        else
            _stack = [_fieldData];
        _stack.VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true);
        _stack.HorizontalOptions = new LayoutOptions(LayoutAlignment.Start, true);
        //_stack.Padding = new Thickness(3, 0, 3, 0);
        //_stack.Margin = new Thickness(3, 0, 3, 0);
        ValueMode = FieldSizeMode.Default;
        Content = _stack;
    }


    public FieldSizeMode ValueMode
    {
        set => _fieldData.MaximumWidthRequest = _fieldData.MinimumWidthRequest = SizeModeToSize(value);
    }

}
