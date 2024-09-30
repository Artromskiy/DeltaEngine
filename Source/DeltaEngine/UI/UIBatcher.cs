using Arch.Core;
using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering.Collections;
using Delta.Runtime;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Delta.UI;

internal class UIBatcher
{
    private readonly Stack<int> _free;
    public GpuArray<BorderData> Borders { get; private set; }
    public GpuArray<int> BordersIds { get; private set; }

    private static readonly QueryDescription _removeEntityDescription = new QueryDescription().WithAll<Border, BorderId, DestroyFlag>();
    private static readonly QueryDescription _removeRenderDescription = new QueryDescription().WithAll<BorderId>().WithNone<Border>();
    private static readonly QueryDescription _addDescription = new QueryDescription().WithAll<Border>().WithNone<BorderId>();
    private static readonly QueryDescription _renderDescription = new QueryDescription().WithAll<Border, BorderId>();
    private static readonly QueryDescription _trsDescriptionTransform = new QueryDescription().WithAll<Border, BorderId>();

    private readonly RemoveEntity _removeEntity;
    private readonly RemoveRender _removeRender;
    private readonly AddRender _addRender;
    private readonly SortWriteRender _sortWriteRender;
    private readonly WriteTrs _writeTrs;

    public UIBatcher()
    {
        Borders = new(1);
        BordersIds = new(1);

        _free = new Stack<int>(Borders.Length);
        for (int i = (Borders.Length - 1); i >= 0; i--)
            _free.Push(i);

        _removeEntity = new RemoveEntity(_free);
        _removeRender = new RemoveRender(_free);
        _addRender = new AddRender(_free);
        _sortWriteRender = new SortWriteRender(BordersIds);
        _writeTrs = new WriteTrs(Borders);
    }


    public void Execute()
    {
        BufferResize();

        _removeEntity.Execute();
        _removeRender.Execute();
        _addRender.Execute();
        _sortWriteRender.Execute();
        _writeTrs.Execute();
    }

    /// <summary>
    /// Ensures capacity of buffers to fit new <see cref="Render"/>s
    /// </summary>
    private void BufferResize()
    {
        var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
        var countToAdd = world.CountEntities(_addDescription);
        var countToRemove = world.CountEntities(_removeRenderDescription);
        var wholeFree = _free.Count + countToRemove;
        var delta = countToAdd - wholeFree;
        if (delta > 0)
        {
            var length = Borders.Length;
            var newLength = (int)BitOperations.RoundUpToPowerOf2((uint)(length + delta));
            Borders.Resize(newLength);
            BordersIds.Resize(newLength);
            _free.EnsureCapacity(newLength);
            for (int i = newLength - 1; i >= length; i--)
                _free.Push(i);
        }
    }

    private readonly struct RemoveEntity(Stack<int> freeIds) : ISystem
    {
        [Imp(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            if (!world.Has(_removeEntityDescription))
                return;
            InlineRemover remover = new(freeIds);
            world.InlineQuery<InlineRemover, BorderId>(_removeEntityDescription, ref remover);
            world.Remove<BorderId, Border>(_removeEntityDescription);
        }

        private readonly struct InlineRemover(Stack<int> free)
            : IForEach<BorderId>
        {
            [Imp(Inl)]
            public readonly void Update(ref BorderId id) => free.Push(id.borderId);
        }
    }

    private readonly struct RemoveRender(Stack<int> freeIds) : ISystem
    {
        [Imp(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            if (!world.Has(_removeRenderDescription))
                return;
            InlineRemover remover = new(freeIds);
            world.InlineQuery<InlineRemover, BorderId>(_removeRenderDescription, ref remover);
            world.Remove<BorderId>(_removeRenderDescription);
        }

        private readonly struct InlineRemover(Stack<int> free)
            : IForEach<BorderId>
        {
            [Imp(Inl)]
            public readonly void Update(ref BorderId id)
            {
                free.Push(id.borderId);
            }
        }
    }

    private readonly struct AddRender(Stack<int> free) : ISystem
    {
        private static readonly QueryDescription _addTag = new QueryDescription().WithAll<AddTag>();
        private readonly struct AddTag();

        [Imp(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            if (!world.Has(_addDescription))
                return;
            // Add not yet registered entities
            // We also tagging all not registered entities
            // and then remove this tag, as it's faster than CommandBuffer
            world.Add<AddTag, BorderId>(_addDescription);
            InlineAdder adder = new(free);
            world.InlineQuery<InlineAdder, Border, BorderId>(_addTag, ref adder);
            world.Remove<AddTag>(_addTag);
        }

        private struct InlineAdder(
            Stack<int> free)
            : IForEach<Border, BorderId>
        {
            [Imp(Inl)]
            public readonly void Update(ref Border render, ref BorderId id)
            {
                int index = free.Pop();
                id = new BorderId(index);
            }
        }
    }

    private readonly struct SortWriteRender(
        GpuArray<int> rendersArray) : ISystem
    {
        [Imp(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            var offsetsCount = 1;
            var offsets = ArrayPool<int>.Shared.Rent(offsetsCount);
            RenderWriter writer = new(rendersArray.Writer, offsets);
            world.InlineQuery<RenderWriter, BorderId>(_renderDescription, ref writer);
            ArrayPool<int>.Shared.Return(offsets);
        }

        private readonly struct RenderWriter(PointerWriter<int> writer, int[] offsets) :
            IForEach<BorderId>
        {
            [Imp(Inl)]
            public readonly void Update(ref BorderId id) => writer[offsets[0]++] = id.borderId;
        }
    }

    private readonly struct WriteTrs(GpuArray<BorderData> trs) : ISystem
    {
        [Imp(Inl)]
        public readonly void Execute()
        {
            var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
            var writer = trs.Writer;
            TrsWriterWithTransform trsWithTransform = new(writer);
            world.InlineParallelEntityQuery<TrsWriterWithTransform, BorderId, Border>(_trsDescriptionTransform, ref trsWithTransform);
        }

        private readonly struct TrsWriterWithTransform(PointerWriter<BorderData> trsArray) :
            IForEachWithEntity<BorderId, Border>
        {
            [Imp(Inl)]
            public readonly void Update(Entity entity, ref BorderId rendId, ref Border border)
            {
                trsArray[rendId.borderId] = new BorderData();// border;
            }
        }
    }


    /// <summary>
    /// Specifies unique id of <see cref="Render"/>.
    /// </summary>
    /// <param name="borderId">index of Transform in TRS buffer</param>
    [DebuggerDisplay($"{nameof(borderId)} = {{{nameof(borderId)}}}")]
    private readonly struct BorderId(int borderId)
    {
        public readonly int borderId = borderId;
    }

    public struct BorderData
    {
        public Vector4 minMax;
        public Vector4 cornerRadius;
        public Vector4 borderThickness;
        public Vector4 color1;
        public Vector4 color2;
        public Vector4 color3;
        public Vector4 color4;
        public Vector4 borderColor1;
        public Vector4 borderColor2;
        public Vector4 borderColor3;
        public Vector4 borderColor4;
    }
}
