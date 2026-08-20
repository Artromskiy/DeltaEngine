# DeltaEngine and DeltaEditor split

The current workspace does not contain a separate `DeltaEditor` repository yet.
The boundary is nevertheless already visible in the project graph:

| Current project | Future owner | Current status |
| --- | --- | --- |
| `Delta.Engine.Integration` | DeltaEngine | Neutral lifecycle, render, world, script DTO and accessor contracts; no Roslyn, Avalonia, Arch or backend references |
| `Delta.Engine` | DeltaEngine | Legacy runtime and Arch migration host; references Integration and Delta.Maths, but not editor packages |
| `Delta.Engine.Windowed` | DeltaEngine composition | Optional SDL3/Delta.Render/Delta.Shader/MoltenVK composition; headless core stays independent |
| `Delta.Editor.Scripting` | DeltaEditor | Roslyn compiler backend, diagnostics, byte/PDB result and collectible compilation implementation |
| `Delta.Engine.EditorLib` | DeltaEditor adapter | Transitional project scanner, hot-reload host, generated Arch accessor backend, shader/model import helpers |
| `Delta.Engine.Editor` | DeltaEditor | Legacy Avalonia shell and current hierarchy/inspector UI; hierarchy is intentionally not part of the first extraction |
| `Delta.Engine.Runner` | DeltaEngine or DeltaEditor, decision pending | Current executable directly references EditorLib and is therefore not a clean runtime entry point |
| `Delta.Engine.ConsoleModelImporter` | DeltaEditor tools | Current importer directly references EditorLib and editor import dependencies |

## Target dependency direction

```text
DeltaEngine.Integration contracts
        ^
        |
DeltaEngine runtime  <-  DeltaEngine windowed composition -> Delta.Render / Delta.Shader / SDL3
        ^
        |
DeltaEditor host -> Roslyn, project scanning, collectible reload, accessors backend, importers, future XAML UI
```

The arrow is dependency direction. DeltaEngine must never reference
DeltaEditor. DeltaEditor may reference the published Integration contracts and,
where needed during migration, the runtime assembly. The reverse reference is
the legacy coupling that must be removed before a physical repository split.

## Safe extraction order

1. Freeze `Delta.Engine.Integration` public contracts and keep the boundary test
   green. These types are the only API that a new DeltaEditor repository may
   consume initially.
2. Extract `Delta.Editor.Scripting` first, preserving assembly and namespace
   identity in the first external commit. Its only engine dependency should be
   the Integration contract package/project reference. Move its tests with it.
3. Extract the compiler host from `Delta.Engine.EditorLib`: project scanning,
   `AssemblyLoadContext` lifecycle, diagnostics presentation and reload policy.
   Keep a small compatibility adapter in the old EditorLib until the new host
   has a green compile/reload gate.
4. Extract generated accessor implementation and inspector metadata binding.
   Keep `ComponentSchema`/`ComponentAccessorTree` as engine-facing value-level
   contracts; do not expose Arch `EntityReference`, raw pointers or Avalonia
   types through them. Generated accessors can remain a DeltaEditor backend.
5. Move model/shader import tools and the Avalonia shell to DeltaEditor. Do not
   move the old hierarchy as an architectural contract; replace it later with
   a producer-facing UI draw list and a future DeltaECS adapter.
6. Reclassify or replace `Delta.Engine.Runner` and
   `Delta.Engine.ConsoleModelImporter`. They are currently editor-bound
   executables, not evidence that DeltaEngine runtime owns editor dependencies.
7. Only after the external repository builds against the published contracts,
   remove the transitional EditorLib references and delete the old Avalonia
   projects from this repository. Preserve history with a file-preserving move
   or subtree extraction, not a simultaneous namespace/API rewrite.

## Current blockers and decisions

- `Delta.Engine.EditorLib` still uses `Delta.Engine.Runtime`, Arch component
  attributes and legacy accessor interfaces. Removing that edge now would be a
  rewrite, so it remains an explicit adapter boundary.
- `Delta.Engine.Runner` and `Delta.Engine.ConsoleModelImporter` still pull
  EditorLib into executable graphs. They should be migrated after the new
  DeltaEditor host has a replacement entry point.
- Delta.Render currently exposes the graphics session needed by the windowed
  adapter, but its worktree is dirty at review time. Engine should consume the
  checked/stable contract, not copy or pin intermediate implementation details.
- DeltaShader has corresponding dirty graphics frontend/compiler changes. The
  Engine project currently consumes only its abstractions and checked shader
  artifacts; runtime Roslyn/MSBuild remains forbidden.
- Delta.Maths is the stable clean producer at the time of this plan and remains
  the engine math dependency.
- Avalonia and the old hierarchy remain migration-only. They are not added to
  neutral runtime contracts and are not expanded by this split.

This plan intentionally does not create a fake `DeltaEditor` project inside
DeltaEngine. The first physical extraction should happen as a separate sibling
repository once the contract package/version and dependency checkout strategy
are agreed; until then, the project graph and tests above provide a reversible
proof of direction.

