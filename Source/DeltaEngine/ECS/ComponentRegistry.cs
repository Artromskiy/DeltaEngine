using Arch.Core;
using Arch.Core.Extensions;
using DeltaEngine.Collections;

namespace DeltaEngine.ECS;
internal class ComponentRegistry<T>
{
    protected readonly World _world;
    protected readonly StackList<T> _components = new();

    public ComponentRegistry(World world)
    {
        _world = world;
        _world.Add<VersId<T>>(new QueryDescription().WithAll<T>());
        var query = new QueryDescription().WithAll<VersId<T>, T>();
        _world.Query(query, (ref VersId<T> x, ref T component) =>
        {
            x = _components.Next();
            _components.Add(component);
        });
        _world.SubscribeComponentAdded<T>(OnComponentAdded);
        _world.SubscribeComponentRemoved<T>(OnComponentRemoved);
        _world.SubscribeComponentSet<T>(OnComponentChanged);
    }

    private void OnComponentAdded(in Entity entity, ref T component)
    {
        var nextId = _components.Next();
        entity.Add(nextId);
        _components.Add(component);
    }

    private void OnComponentRemoved(in Entity entity, ref T component)
    {
        ref var id = ref entity.TryGetRef<VersId<T>>(out var has);
        Debug.Assert(has);
        _components.RemoveAt(id.id);
        entity.Remove<VersId<Transform>>();
    }

    private static void OnComponentChanged(in Entity entity, ref T component)
    {
        entity.AddOrGet<DirtyFlag<T>>();
    }
}