using Arch.Core;
using Delta.Runtime;

namespace DeltaEditor.Gizmos
{
    internal class TransformGizmos
    {
        private EntityReference _selectedEntity;
        public void SelectEntity(EntityReference entity)
        {
            _selectedEntity = entity;
        }

        public void UpdateTransformGizmos(IRuntime runtime)
        {
            //runtime.Context.GraphicsModule.InstantDraw(selectedEntity.Entity)
        }
    }
}
