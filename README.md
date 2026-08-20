# DeltaEngine

Composition and runtime layer for the Furnace stack. The target is one C# game
and editor runtime using Delta.Maths, DeltaECS, DeltaShader, DeltaRender,
DeltaXAML and SDL3-CS.

## Ownership

DeltaEngine owns frame timing, SDL event polling, input translation, close and
resize handling, scene/runtime orchestration and adapters between subsystems.
It does not own Vulkan resource implementation, shader compilation, retained UI
layout, or ECS storage.

```text
DeltaEngine host
  -> DeltaECS simulation
  -> DeltaXAML frame/layout/input
  -> DeltaRender extraction and presentation
```

Headless `Delta.Engine.Integration` remains independent of SDL, Vulkan,
DeltaRender and DeltaShader. Editor-owned Roslyn scripting is being extracted to
the sibling DeltaEditor repository. Avalonia and Arch are migration-only code;
new runtime work must not deepen those dependencies.

## Current integration priorities

1. Compose real ShaderArtifacts and DeltaRender in the windowed host.
2. Adapt DeltaXAML draw lists without moving UI ownership into the engine.
3. Preserve a headless integration path for tests and tooling.
4. Remove compatibility copies only after all consumers migrate.

Hierarchy redesign is not part of the current split.

## Build and test

```bash
dotnet build Source/Delta.Engine.Windowed/Delta.Engine.Windowed.csproj \
  -c Release --no-restore --disable-build-servers -m:1 \
  /p:UseSharedCompilation=false
dotnet build Source/Delta.Engine.slnx -c Release --no-restore \
  --disable-build-servers -m:1 /p:UseSharedCompilation=false -v:minimal
dotnet test Source/Delta.Engine.slnx -c Release --no-build --no-restore \
  --disable-build-servers -m:1
```

Known legacy warnings include nullable/Avalonia diagnostics, the old generator
dependency load warning, and ImageSharp advisories. Keep dependency upgrades in
separate changes. Benchmarks are manual until the legacy graph is removed.
