using Arch.Core;
using DVG.Engine.ECS.Components;
using DVG.Engine.Runtime;

using DVG.Engine.ECS;
internal static class DestroySystem
{
    private static readonly QueryDescription _destroyDescription = new QueryDescription().WithAll<DestroyFlag>();
    public static void Execute()
    {
        IRuntimeContext.Current.SceneManager.CurrentScene._world.Destroy(_destroyDescription);
    }
}
