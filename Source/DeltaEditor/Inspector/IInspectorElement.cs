using Arch.Core;

namespace DeltaEditor.Inspector
{
    internal interface IInspectorElement : IView
    {
        public void UpdateData(EntityReference entity);
    }
}
