using System.Collections.Generic;

namespace Delta.Engine.Integration;

public enum EngineWorldChangeKind
{
    ComponentChanged,
    TopologyChanged,
    EntityDestroyed,
}

/// <summary>
/// A renderer-facing description of one world change. ComponentId is null for
/// changes that do not address a component, such as topology or destruction.
/// </summary>
public readonly record struct EngineWorldChange(
    ulong EntityId,
    string? ComponentId,
    EngineWorldChangeKind Kind);

/// <summary>
/// Records mutations without imposing an ECS storage or scheduler model.
/// Read-only world access must not call this boundary.
/// </summary>
public interface IEngineWorldChangeRecorder
{
    void Record(in EngineWorldChange change);
}

/// <summary>
/// Supplies changes to one named consumer. Consuming this subscription does
/// not consume any other subscription.
/// </summary>
public interface IEngineWorldChangeSource
{
    IEngineWorldChangeSubscription Subscribe(string consumerId);
}

public interface IEngineWorldChangeSubscription : IDisposable
{
    /// <summary>
    /// Returns an owned batch and removes only this subscription's pending
    /// changes. The memory is valid until the batch is disposed.
    /// </summary>
    IEngineWorldChangeBatch Consume();
}

public interface IEngineWorldChangeBatch : IDisposable
{
    ReadOnlyMemory<EngineWorldChange> Changes { get; }
}

/// <summary>
/// The temporary bridge from a consumer-owned world change subscription to a
/// renderer. The renderer receives changes only for the current frame and
/// never owns input or world dirty state.
/// </summary>
public interface IEngineRenderChangeConsumer
{
    void Render(EngineFrameContext frame, ReadOnlySpan<EngineWorldChange> changes);
}

public sealed class EngineWorldChangeRenderAdapter
{
    public void Render(
        EngineFrameContext frame,
        IEngineWorldChangeSubscription subscription,
        IEngineRenderChangeConsumer renderer)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(renderer);

        using IEngineWorldChangeBatch batch = subscription.Consume();
        renderer.Render(frame, batch.Changes.Span);
    }
}

/// <summary>
/// A small engine-owned adapter useful while ECS structural APIs are in flux.
/// Each active consumer receives its own copy of every recorded change.
/// </summary>
public sealed class EngineWorldChangeJournal : IEngineWorldChangeRecorder, IEngineWorldChangeSource, IDisposable
{
    private readonly Dictionary<string, ConsumerQueue> _consumers = new(StringComparer.Ordinal);
    private bool _disposed;

    public IEngineWorldChangeSubscription Subscribe(string consumerId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerId);

        if (!_consumers.TryAdd(consumerId, new ConsumerQueue(this, consumerId)))
        {
            throw new InvalidOperationException($"Consumer '{consumerId}' is already subscribed.");
        }

        return _consumers[consumerId];
    }

    public void Record(in EngineWorldChange change)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (ConsumerQueue consumer in _consumers.Values)
        {
            consumer.Pending.Add(change);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (ConsumerQueue consumer in _consumers.Values)
        {
            consumer.DisposeFromOwner();
        }

        _consumers.Clear();
    }

    private void Unsubscribe(ConsumerQueue consumer)
    {
        if (_consumers.Remove(consumer.ConsumerId))
        {
            consumer.DisposeFromOwner();
        }
    }

    private sealed class ConsumerQueue : IEngineWorldChangeSubscription
    {
        private readonly EngineWorldChangeJournal _owner;
        private bool _disposed;

        public ConsumerQueue(EngineWorldChangeJournal owner, string consumerId)
        {
            _owner = owner;
            ConsumerId = consumerId;
        }

        public string ConsumerId { get; }

        public List<EngineWorldChange> Pending { get; } = new();

        public IEngineWorldChangeBatch Consume()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            EngineWorldChange[] changes = Pending.ToArray();
            Pending.Clear();
            return new OwnedChangeBatch(changes);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _owner.Unsubscribe(this);
            }
        }

        public void DisposeFromOwner()
        {
            _disposed = true;
            Pending.Clear();
        }
    }

    private sealed class OwnedChangeBatch : IEngineWorldChangeBatch
    {
        private ReadOnlyMemory<EngineWorldChange> _changes;
        private bool _disposed;

        public OwnedChangeBatch(EngineWorldChange[] changes)
        {
            _changes = changes;
        }

        public ReadOnlyMemory<EngineWorldChange> Changes
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return _changes;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _changes = ReadOnlyMemory<EngineWorldChange>.Empty;
            }
        }
    }
}
