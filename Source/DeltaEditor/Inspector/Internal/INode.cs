using Arch.Core;

namespace DeltaEditor.Inspector.Internal;

internal interface INode : IView
{
    public void UpdateData(EntityReference entity);
}
