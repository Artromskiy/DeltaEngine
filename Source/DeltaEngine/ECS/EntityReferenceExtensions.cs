using Arch.Core;
using Delta.Runtime;

namespace Delta.ECS;
public static class EntityReferenceExtensions
{
    public static bool IsAlive(this EntityReference entityRef)
    {
        return entityRef.IsAlive(IRuntimeContext.Current.SceneManager.CurrentScene._world);
    }
}
