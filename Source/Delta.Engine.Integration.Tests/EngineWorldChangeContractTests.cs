using Delta.Engine.Integration;
using Xunit;

namespace Delta.Engine.Integration.Tests;

public sealed class EngineWorldChangeContractTests
{
    [Fact]
    public void ReadOnlyAccessDoesNotCreateDirtyChange()
    {
        using var journal = new EngineWorldChangeJournal();
        using IEngineWorldChangeSubscription subscription = journal.Subscribe("renderer");
        var world = new FakeWorld(journal);

        _ = world.ReadOnlyValue;

        using IEngineWorldChangeBatch batch = subscription.Consume();
        Assert.Empty(batch.Changes.ToArray());
    }

    [Fact]
    public void MutableAccessIsVisibleToRendererInTheSameFrame()
    {
        using var journal = new EngineWorldChangeJournal();
        using IEngineWorldChangeSubscription subscription = journal.Subscribe("renderer");
        var world = new FakeWorld(journal);
        var renderer = new FakeRenderer();
        var adapter = new EngineWorldChangeRenderAdapter();

        ref int value = ref world.GetMutableValue(7, "Transform");
        value = 42;
        adapter.Render(default, subscription, renderer);

        var change = Assert.Single(renderer.LastChanges);
        Assert.Equal((ulong)7, change.EntityId);
        Assert.Equal("Transform", change.ComponentId);
        Assert.Equal(EngineWorldChangeKind.ComponentChanged, change.Kind);
    }

    [Fact]
    public void RendererReadsOnlyItsSubscription()
    {
        using var journal = new EngineWorldChangeJournal();
        using IEngineWorldChangeSubscription rendererSubscription = journal.Subscribe("renderer");
        using IEngineWorldChangeSubscription inspectorSubscription = journal.Subscribe("inspector");
        var renderer = new FakeRenderer();
        var world = new FakeWorld(journal);

        _ = world.GetMutableValue(3, "Position");
        new EngineWorldChangeRenderAdapter().Render(default, rendererSubscription, renderer);

        Assert.Single(renderer.LastChanges);
        using IEngineWorldChangeBatch inspectorBatch = inspectorSubscription.Consume();
        Assert.Single(inspectorBatch.Changes.ToArray());
    }

    [Fact]
    public void ReadingOneConsumerDoesNotClearAnotherConsumer()
    {
        using var journal = new EngineWorldChangeJournal();
        using IEngineWorldChangeSubscription first = journal.Subscribe("first");
        using IEngineWorldChangeSubscription second = journal.Subscribe("second");

        journal.Record(new EngineWorldChange(11, "Mesh", EngineWorldChangeKind.ComponentChanged));
        using IEngineWorldChangeBatch firstBatch = first.Consume();
        using IEngineWorldChangeBatch secondBatch = second.Consume();

        Assert.Single(firstBatch.Changes.ToArray());
        Assert.Single(secondBatch.Changes.ToArray());
    }

    [Fact]
    public void TopologyAndDestroyChangesHaveSeparateKinds()
    {
        using var journal = new EngineWorldChangeJournal();
        using IEngineWorldChangeSubscription subscription = journal.Subscribe("renderer");

        journal.Record(new EngineWorldChange(9, null, EngineWorldChangeKind.TopologyChanged));
        journal.Record(new EngineWorldChange(9, null, EngineWorldChangeKind.EntityDestroyed));

        using IEngineWorldChangeBatch batch = subscription.Consume();
        EngineWorldChange[] changes = batch.Changes.ToArray();
        Assert.Equal(2, changes.Length);
        Assert.Equal(EngineWorldChangeKind.TopologyChanged, changes[0].Kind);
        Assert.Equal(EngineWorldChangeKind.EntityDestroyed, changes[1].Kind);
    }

    private sealed class FakeWorld
    {
        private readonly IEngineWorldChangeRecorder _recorder;
        private int _value;

        public FakeWorld(IEngineWorldChangeRecorder recorder)
        {
            _recorder = recorder;
        }

        public int ReadOnlyValue => _value;

        public ref int GetMutableValue(ulong entityId, string componentId)
        {
            _recorder.Record(new EngineWorldChange(
                entityId,
                componentId,
                EngineWorldChangeKind.ComponentChanged));
            return ref _value;
        }
    }

    private sealed class FakeRenderer : IEngineRenderChangeConsumer
    {
        public EngineWorldChange[] LastChanges { get; private set; } = Array.Empty<EngineWorldChange>();

        public void Render(EngineFrameContext frame, ReadOnlySpan<EngineWorldChange> changes)
        {
            LastChanges = changes.ToArray();
        }
    }
}
