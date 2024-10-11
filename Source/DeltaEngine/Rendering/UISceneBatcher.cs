using Arch.Core;
using Delta.Assets;
using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering.Collections;
using Delta.Runtime;
using Delta.Utilities;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using static Delta.Rendering.UISceneBatcher;

namespace Delta.Rendering;

internal class UISceneBatcher : GenericBatcher<BorderData, int, SceneData>
{
    private readonly Stack<int> _free;
    private static readonly int[][] _bindings = [[0, 1], [0]];
    public override JaggedReadOnlySpan<int> DescriptorSetsBindings => _bindings;
    private GpuArray<BorderData> Borders => _bufferT0;
    private GpuArray<int> BordersIds => _bufferT1;
    private GpuArray<SceneData> Scene => _bufferT2;

    private (Render rend, int count) _renderGroup;
    public override ReadOnlySpan<(Render rend, int count)> RendGroups => _renderGroup.count == 0 ?
        [] : new(ref _renderGroup);

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

    private static GuidAsset<MaterialData> _borderMaterial;
    private static GuidAsset<MaterialData> BorderMaterial
    {
        get
        {
            if (_borderMaterial.Null)
            {
                var assimp = IRuntimeContext.Current.AssetImporter;
                var materials = assimp.GetAssets<MaterialData>();
                _borderMaterial = Array.Find(materials, x => assimp.GetName(x) == "BorderMaterial");
            }
            return _borderMaterial;
        }
    }
    private static GuidAsset<MeshData> _borderMesh;
    private static GuidAsset<MeshData> BorderMesh
    {
        get
        {
            if (_borderMesh.Null)
            {
                var assimp = IRuntimeContext.Current.AssetImporter;
                _borderMesh = assimp.CreateAsset<MeshData>(new(4, [0, 1, 2, 2, 3, 0]), "EmptyQuad.mesh");
            }
            return _borderMesh;
        }
    }

    public UISceneBatcher()
    {
        _free = new Stack<int>(Borders.Length);
        for (int i = Borders.Length - 1; i >= 0; i--)
            _free.Push(i);

        _removeEntity = new RemoveEntity(_free);
        _removeRender = new RemoveRender(_free);
        _addRender = new AddRender(_free);
        _sortWriteRender = new SortWriteRender(BordersIds);
        _writeTrs = new WriteTrs(Borders);
    }


    public override void Execute()
    {
        BufferResize();

        _removeEntity.Execute();
        _removeRender.Execute();
        _addRender.Execute();
        _sortWriteRender.Execute();
        _writeTrs.Execute();

        var world = IRuntimeContext.Current.SceneManager.CurrentScene._world;
        var (width, height) = IRuntimeContext.Current.GraphicsModule.Size;
        var rendersCount = world.CountEntities(_renderDescription);
        _renderGroup = (new Render(BorderMaterial, BorderMesh, true), rendersCount);
        Scene.Writer[0] = new SceneData()
        {
            windowSize = new Vector4(width, height, 0, 0)
        };
    }

    public override void Dispose() => throw new NotImplementedException();

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
            var newLength = length + delta;
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
            Array.Clear(offsets);
            RenderWriter writer = new(rendersArray.Writer, offsets);
            Debug.Assert(world.CountEntities(_renderDescription) <= 1);
            foreach (var item in offsets)
                Debug.Assert(item == 0);
            world.InlineQuery<RenderWriter, BorderId>(_renderDescription, ref writer);
            ArrayPool<int>.Shared.Return(offsets, true);
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
                trsArray[rendId.borderId] = new BorderData()
                {
                    minMax = border.minMax,
                    colorsRgba = (uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue),
                    borderColorsRgba = (uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue),
                    borderThickness = new(0, 0, 0, 0),
                    cornerRadius = new(3, 3, 3, 3)
                };// border;
                Console.WriteLine($"{3}");
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
        public (uint, uint, uint, uint) colorsRgba;
        public (uint, uint, uint, uint) borderColorsRgba;
    }

    public struct SceneData
    {
        public Matrix4x4 projView;
        public Matrix4x4 proj;
        public Matrix4x4 view;
        public Vector4 position;
        public Vector4 rotation;
        public Vector4 windowSize;
    };
}
