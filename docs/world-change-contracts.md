# World-to-render change contract

This is a thin engine-owned boundary for the period while the ECS structural
API is changing. It does not prescribe ECS storage, command buffers, a global
barrier, a scheduler, or an event bus.

## Frame flow

The intended single-thread flow is:

`world update -> renderer consumes its subscription -> render the same frame`

`EngineWorldChangeJournal` is an adapter and testable reference implementation.
Each consumer subscribes by a unique id. A recorded change is copied to every
active subscription. Consuming one subscription removes only that subscription's
pending changes.

## Change record

`EngineWorldChange` identifies an entity, an optional component id, and one of:

- `ComponentChanged`: component data was mutably accessed or changed.
- `TopologyChanged`: entity/component topology changed.
- `EntityDestroyed`: the entity was destroyed.

Topology and destruction are separate record kinds so a future ECS adapter can
preserve their ordering and semantics without exposing ECS types here.

## Lifetime and dirty ownership

`IEngineWorldChangeSubscription.Consume()` returns an owned
`IEngineWorldChangeBatch`. Its `ReadOnlyMemory<EngineWorldChange>` is valid until
the batch is disposed. The render adapter exposes the batch as a
`ReadOnlySpan<EngineWorldChange>` only for the synchronous render call.

Readonly world access must not call `IEngineWorldChangeRecorder`. Mutable access
must record the addressed entity/component before returning mutable storage. The
renderer owns neither input nor world dirty state, and reading its subscription
does not clear any other consumer's selection.

The future ECS adapter may replace the journal with consumer-owned tracking, as
long as it preserves these contracts and does not require a global clear.
