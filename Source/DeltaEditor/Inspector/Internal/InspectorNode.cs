using Arch.Core;
using Avalonia.Controls;

namespace DeltaEditor.Inspector.Internal;

internal abstract class InspectorNode : UserControl, INode
{
    public abstract bool UpdateData(ref EntityReference entity);
}
