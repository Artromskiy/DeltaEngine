using Arch.Core;
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
    }
}