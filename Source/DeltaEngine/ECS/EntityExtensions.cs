using Arch.Core;
using Delta.Runtime;
using System.Diagnostics;

namespace Delta.ECS;

public static class EntityExtensions
{
    public static bool Has<T>(this Entity entity)
    {
        Debug.Assert(IRuntimeContext.Current.SceneManager.CurrentScene != null);
        return IRuntimeContext.Current.SceneManager.CurrentScene._world.Has<T>(entity);
    }

    public static ref T Get<T>(this Entity entity)
    {
        Debug.Assert(IRuntimeContext.Current.SceneManager.CurrentScene != null);
        return ref IRuntimeContext.Current.SceneManager.CurrentScene._world.Get<T>(entity);
    }

    public static ref T TryGetRef<T>(this Entity entity, out bool has)
    {
        Debug.Assert(IRuntimeContext.Current.SceneManager.CurrentScene != null);
        return ref IRuntimeContext.Current.SceneManager.CurrentScene._world.TryGetRef<T>(entity, out has);
    }
    public static int Version(this Entity entity)
    {
        Debug.Assert(IRuntimeContext.Current.SceneManager.CurrentScene != null);
        return IRuntimeContext.Current.SceneManager.CurrentScene._world.Version(entity);
    }
}
