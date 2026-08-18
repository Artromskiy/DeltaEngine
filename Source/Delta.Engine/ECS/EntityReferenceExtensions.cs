using Arch.Core;
using Delta.Engine.Runtime;

using Delta.Engine.ECS;
public static class EntityReferenceExtensions
{
    public static bool IsAlive(this EntityReference entityRef)
    {
        return entityRef.IsAlive(IRuntimeContext.Current.SceneManager.CurrentScene._world);
    }
}
