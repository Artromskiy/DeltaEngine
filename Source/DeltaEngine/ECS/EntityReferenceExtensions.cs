using Arch.Core;
using DVG.Engine.Runtime;

using DVG.Engine.ECS;
public static class EntityReferenceExtensions
{
    public static bool IsAlive(this EntityReference entityRef)
    {
        return entityRef.IsAlive(IRuntimeContext.Current.SceneManager.CurrentScene._world);
    }
}
