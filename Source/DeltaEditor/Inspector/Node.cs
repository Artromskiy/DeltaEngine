using Arch.Core;

namespace DeltaEditor.Inspector;

internal abstract class Node : ContentView, INode
{
    protected const double NodeHeight = 30;
    protected readonly Label _fieldName = new()
    {
        VerticalTextAlignment = TextAlignment.Center,
        HorizontalTextAlignment = TextAlignment.Start,
        MinimumHeightRequest = NodeHeight,
        MaximumHeightRequest = NodeHeight,
        BackgroundColor = NodeConst.BackColor,
    };
    protected readonly NodeData _nodeData;
    protected virtual bool SuppressTypeCheck => false;

    public Node(NodeData nodeData)
    {
        _nodeData = nodeData;
        _fieldName.Text = nodeData.FieldName;
        NameMode = FieldSizeMode.Default;
        BackgroundColor = NodeConst.BackColor;
    }
    public abstract void UpdateData(EntityReference entity);

    public FieldSizeMode NameMode
    {
        set => _fieldName.MaximumWidthRequest = _fieldName.MinimumWidthRequest = SizeModeToSize(value);
    }
    protected static double SizeModeToSize(FieldSizeMode sizeMode)
    {
        return sizeMode switch
        {
            FieldSizeMode.Default => 80,
            FieldSizeMode.Small => 50,
            FieldSizeMode.ExtraSmall => 30,
            FieldSizeMode.Large => 200,
            FieldSizeMode.ExtraLarge => 300,
            _ => throw new NotImplementedException(),
        };
    }
}

internal abstract class Node<T> : Node
{
    public Node(NodeData nodeData) : base(nodeData)
    {
        if (!SuppressTypeCheck)
            ThrowHelper.CheckTypes(_nodeData);
    }

    public T GetData(EntityReference entity) => _nodeData.GetData<T>(entity);
    public void SetData(EntityReference entity, T value) => _nodeData.SetData(entity, value);


    private static class ThrowHelper
    {
        public static void CheckTypes(NodeData nodeData)
        {
            var type = nodeData.FieldType;
            if (type != typeof(T))
                throw new InvalidOperationException($"Type of field is not{nameof(T)} in path {string.Join(",", nodeData.Path.ToArray())}");
        }
    }
}
