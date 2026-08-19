# Legacy core build blocker

The legacy `Delta.Engine` build remains manual-only. Its project graph still
references the vendored `Depend/Arch/src/Arch/Arch.csproj` and the owned
source-generator projects as analyzers:

```text
Delta.Engine -> Arch
Delta.Engine -> Delta.Engine.Generation (Analyzer)
Delta.Engine -> Delta.Engine.Generation.Internal (Analyzer)
```

The current Arch source exposes `World.IsAlive(Entity)`,
`World.IsAlive(EntityReference)`, and `EntityReference.IsAlive(World)`. The
engine-side `EntityReferenceExtensions` calls the reference/world overload, so
the previously reported missing-`IsAlive` source error is not reproducible at
this checkout. The remaining blocker is the legacy Arch/analyzer build graph
itself: a bounded Release core build with project references and analyzers
disabled still does not reach a compiler result within 30 seconds. The only
diagnostic emitted before stopping is `NU1900` from the unavailable NuGet
advisory feed. This note does not change Arch or attempt a core rewrite.

Standalone `Delta.Engine.Integration` remains the automatic gate. The optional
ComputeSmoke project is independent of the legacy core project.
