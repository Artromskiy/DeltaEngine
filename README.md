# DeltaEngine

Runtime composition layer for the Furnace stack. It owns frame timing, SDL
event polling and input translation, close/resize handling, scene/runtime
orchestration and neutral subsystem adapters.

```text
Engine host -> DeltaECS simulation -> DeltaXAML update/layout
  -> DeltaRender extraction/presentation
```

Headless `Delta.Engine.Integration` remains independent of SDL, Vulkan,
DeltaRender and DeltaShader. DeltaEngine does not own Vulkan resources, shader
compilation, XAML parsing/layout, font shaping or editor Roslyn tooling.
DeltaEditor owns scripting and inspection.

Avalonia and Arch are migration-only dependencies; new runtime work must not
deepen them. Durable dependency direction is documented in
[docs/architecture-roadmap.md](docs/architecture-roadmap.md).

See [WORKFLOW.md](WORKFLOW.md) for targeted verification,
[TODO.md](TODO.md) for selected work and [AGENTS.md](AGENTS.md) for routing.
