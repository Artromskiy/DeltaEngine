using Arch.Core;

namespace DeltaEditor.Inspector;

internal interface INode : IView
{
    public void UpdateData(EntityReference entity);
}
