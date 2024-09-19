using Arch.Core;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Delta.Runtime;

namespace DeltaEditor.Inspector.Internal;

internal abstract class InspectorNode : UserControl
{
    public abstract bool UpdateData(ref EntityReference entity, IRuntimeContext ctx);
    protected bool ClipVisible
    {
        get
        {
            var bounds = this.GetTransformedBounds();
            bool visible = bounds.HasValue && bounds.Value.Clip != default;
            return visible;
        }
    }

    public abstract void SetLabelColor(IBrush brush);
}