using Arch.Core;
using DeltaEngine.Collections;
using DeltaEngine.Rendering;

namespace DeltaEngine.ECS;
internal class GpuMappedChilds<C, P> : StorageDynamicArray<uint>
{
    private static readonly QueryDescription _all = new QueryDescription().WithAll<VersId<C>>();
    private static readonly QueryDescription _direct = new QueryDescription().WithAll<VersId<C>, VersId<P>>();
    private static readonly QueryDescription _childed = new QueryDescription().WithAll<VersId<C>, ChildOf>().WithNone<VersId<P>>();
    private static readonly bool _skipDirect = typeof(C).Equals(typeof(P));

    private readonly World _world;

    public GpuMappedChilds(World world, RenderBase renderData) : base(renderData, (uint)world.CountEntities(_all))
    {
        _world = world;
        var writer = GetWriter();
        if (!_skipDirect) // Skip component self referencing
        {
            DirectInlineCreator directCreator = new(writer);
            _world.InlineQuery<DirectInlineCreator, VersId<C>, VersId<P>>(_direct, ref directCreator);
        }
        ChildedInlineCreator childedCreator = new(writer);
        _world.InlineQuery<ChildedInlineCreator, VersId<C>, ChildOf>(_childed, ref childedCreator);
    }

    private readonly struct DirectInlineCreator(Writer writer) : IForEach<VersId<C>, VersId<P>>
    {
        public readonly void Update(ref VersId<C> child, ref VersId<P> parent)
        {
            writer[child.id] = parent.id;
        }
    }
    private readonly struct ChildedInlineCreator(Writer writer) : IForEach<VersId<C>, ChildOf>
    {
        public readonly void Update(ref VersId<C> child, ref ChildOf childOf)
        {
            if (childOf.GetParent(out VersId<P> parent))
                writer[child.id] = parent.id;
        }
    }
}
