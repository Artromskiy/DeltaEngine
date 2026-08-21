# DeltaEngine agent guide

Scope: runtime scheduling, SDL event/input translation, scene orchestration and
neutral adapters. Editor-specific Roslyn/tooling belongs in DeltaEditor.

- [README.md](README.md) — stable runtime ownership.
- [TODO.md](TODO.md) — selected engine work.
- [IDEAS.md](IDEAS.md) — deferred runtime architecture.
- [WORKFLOW.md](WORKFLOW.md) — targeted builds, tests and integration checks.
- Read [docs/architecture-roadmap.md](docs/architecture-roadmap.md) for durable
  dependency direction; read other contract/migration docs only when that
  boundary is in scope.
- [../EDITOR_UI_TODO.md](../EDITOR_UI_TODO.md) is authoritative for the editor
  window/inspector milestone.

Headless integration stays independent of SDL/Vulkan. Renderer never polls
input; Engine never parses shader source, XAML or font outlines.

Skills: `game-developer` for frame/runtime architecture, `static-analysis` for
dependency edges, `concurrency-debugging` for frame/reload ordering,
`apple-silicon` for SDL/MoltenVK composition, and `lldb` for native crashes.
