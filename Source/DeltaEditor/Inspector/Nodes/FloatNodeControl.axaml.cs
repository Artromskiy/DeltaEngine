using Arch.Core;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor;

internal partial class FloatNodeControl : InspectorNode
{
    private readonly NodeData _nodeData;
    public static readonly StyledProperty<HorizontalAlignment> FieldNameAlignmentProperty =
        AvaloniaProperty.Register<ComponentNodeControl, HorizontalAlignment>(nameof(FieldNameAlignment));

    public HorizontalAlignment FieldNameAlignment
    {
        get => GetValue(FieldNameAlignmentProperty);
        set
        {
            SetValue(FieldNameAlignmentProperty, value);
            Field.HorizontalAlignment = value;
        }
    }

    public FloatNodeControl() => InitializeComponent();
    public FloatNodeControl(NodeData nodeData) : this()
    {
        Field.FieldName = (_nodeData = nodeData).FieldName;
        Field.OnDrag += x => _nodeData.DragFloat(Field.FieldData, x, 0.01f);
    }

    public override void SetLabelColor(IBrush brush) {}

    public override bool UpdateData(ref EntityReference entity, IRuntimeContext ctx)
    {
        if (!ClipVisible)
            return false;

        bool changed = _nodeData.UpdateFloat(Field.FieldData, ref entity);

        return changed;
    }
}