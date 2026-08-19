# Legacy core graph status

The legacy `Delta.Engine` core project now has a bounded Release build with no
warnings or errors. Its remaining intentional project references are the
vendored `Depend/Arch/src/Arch/Arch.csproj`, `Delta.Maths`, and the owned
source-generator projects:

```text
Delta.Engine -> Arch
Delta.Engine -> Delta.Engine.Generation (Analyzer)
Delta.Engine -> Delta.Engine.Generation.Core (Analyzer dependency)
```

The current Arch build is not compiled with `PURE_ECS`, so the available
reference API is `EntityReference.IsAlive()`; the engine adapter uses that
instance API and does not modify Arch. The removed `Generation.Internal`
analyzer used to emit the deleted renderer `RenderBatcher`/GPU collection
types. `Generation.Core` remains an explicit analyzer dependency so the
owned `Generation` analyzer can load its shared generator base.

The optional dependent projects still have independent source blockers:
`Delta.Engine.EditorLib` stops at the malformed raw string in
`Scripting/TestCompileFiles.cs`, and `Delta.Engine.Benchmarks` stops at the
malformed namespace declaration in `RefGetBench.cs`. Those files are outside
the analyzer/core graph fix and benchmark workload execution remains disabled.

This note does not change Arch or attempt a core rewrite.

Standalone `Delta.Engine.Integration` remains the automatic gate. The optional
ComputeSmoke project is independent of the legacy core project.
