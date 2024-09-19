using Arch.Core;
using Delta.ECS.Components;
using Delta.Runtime;

namespace Delta.ECS;
internal static class DestroySystem
{
    private static readonly QueryDescription _destroyDescription = new QueryDescription().WithAll<DestroyFlag>();
    public static void Execute()
    {
        if (IRuntimeContext.Current.SceneManager.CurrentScene == null)
            return;
        IRuntimeContext.Current.SceneManager.CurrentScene._world.Destroy(_destroyDescription);
    }
}