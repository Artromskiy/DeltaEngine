# First engine delivery contracts and migration inventory

Date: 2026-08-18
Scope: `DeltaEngine` workstream only.

## Namespace convention

All owned projects in `Source` use the `DVG.Engine` root. The namespace migration
keeps assembly and project names unchanged:

| Previous root | New root |
| --- | --- |
| `Delta` | `DVG.Engine` |
| `DeltaEditor` | `DVG.Engine.Editor` |
| `DeltaEditorLib` | `DVG.Engine.EditorLib` |
| `DeltaGen` | `DVG.Engine.Generation` |
| `DeltaGenCore` | `DVG.Engine.Generation.Core` |
| `DeltaGenInternal` | `DVG.Engine.Generation.Internal` |
| `DeltaGenTest` | `DVG.Engine.Generation.Tests` |
| `DeltaBench` | `DVG.Engine.Benchmarks` |
| `EngineRunner` | `DVG.Engine.Runner` |
| `ConsoleModelImporter` | `DVG.Engine.ConsoleModelImporter` |
| `FirstEngineDelivery.Tests` | `DVG.Engine.Integration.Tests` |

`Depend/Arch` and its namespaces are external/vendored and are intentionally
unchanged. This is a source and public API compatibility break for consumers that
reference the old owned namespaces; assembly names remain stable to limit the
break to source/API binding rather than deployment identity.

## Dependency/migration inventory snapshot

| Layer | Current DeltaEngine dependency | Status | Replacement path |
| --- | --- | --- | --- |
| Runtime storage | `Depend/Arch` project reference in `Source/DeltaEngine/DeltaEngine.csproj` | Kept for migration compatibility | `DeltaECS` must expose contracts used by engine boundaries |
| Vulkan/graphics wrappers | `Silk.NET.Vulkan`, `Silk.NET.Vulkan.Extensions.*` and `Silk.NET.Windowing` in `Source/DeltaEngine/DeltaEngine.csproj` | Kept for compatibility in legacy renderer/runtime | `DeltaRender` should provide SDL3 + Vulkan/MoltenVK implementation through renderer-facing contracts |
| Renderer windowed stack | `DeltaEngine/Rendering` classes (`Windowed*`, `Headless*`) | Present and compile-only in this pass | Replace by `DeltaRender` adapter + sample surface/present |
| Editor composition | `Avalonia` packages in `Source/DeltaEditor/DeltaEditor.csproj` | Kept while migration is incremental | `Delta`-owned XAML runtime and editor UI should replace direct Avalonia usage later |
| Math/shader tooling | `KibiHex.Maths` and GLSH are external references outside this repo | Target stack agreed | Keep contracts only; no direct dependency wiring in engine-facing interfaces |

## Engine-facing contracts added in this pass

Source: `Source/DeltaEngine.Integration/EngineIntegrationContracts.cs`

```csharp
public readonly record struct InputSnapshot(
    int FrameNumber,
    bool ExitRequested = false);

public readonly record struct EngineFrameContext(
    int FrameNumber,
    float DeltaSeconds,
    InputSnapshot Input);

public interface IEngineHost : IDisposable
{
    bool IsRunning { get; }
    bool IsDisposed { get; }
    int CompletedFrames { get; }
    IReadOnlyList<EngineLifecycleEvent> StageLog { get; }
    void Start();
    void RunFrame(float deltaSeconds);
    void Shutdown();
}

public interface IEngineInputService : IDisposable
{
    void Initialize();
    InputSnapshot PollInput(int frameNumber, float deltaSeconds);
    void Shutdown();
}

public interface IEngineWorldService : IDisposable
{
    void Initialize();
    void Update(in EngineFrameContext context);
    void Shutdown();
}

public interface IEngineRenderService : IDisposable
{
    void Initialize();
    void Render(in EngineFrameContext context);
    void Shutdown();
}

public interface IEngineUiService : IDisposable
{
    void Initialize();
    void Update(in EngineFrameContext context);
    void Shutdown();
}
```

No SDL/Vulkan/Avalonia/Arch types are allowed in these signatures.

`IEngineRenderService.Render(EngineFrameContext)` is a temporary adapter boundary
for this first delivery. The renderer must not poll, own, or otherwise coordinate
input; input is polled by `IEngineInputService` and passed through the host. The
permanent renderer boundary will be an extracted `RenderPacket` contract once the
renderer and ECS owners provide those APIs.

## Deterministic lifecycle stages enforced by `EngineHost`

Implemented in `Source/DeltaEngine.Integration/EngineHost.cs`:

1. `InputInitialized`
2. `WorldInitialized`
3. `RenderInitialized`
4. `UiInitialized`
5. For each frame:
   1. `FrameStarted`
   2. `InputPolled`
   3. `WorldUpdated`
   4. `RenderUpdated`
   5. `UiUpdated`
   6. `FrameCompleted`
6. `ShutdownStarted`
7. `InputShutdown`
8. `WorldShutdown`
9. `RenderShutdown`
10. `UiShutdown`
11. `HostDisposalStarted`
12. `UiDisposed`
13. `RenderDisposed`
14. `WorldDisposed`
15. `InputDisposed`
16. `HostDisposed`

This stage order is tested via contract tests.

`Start`, `Shutdown`, and `Dispose` are idempotent. A frame is counted only after
`FrameCompleted`; exceptions from services propagate to the caller and do not
produce a completed frame.

## Contracts requested from ECS owner

`DeltaECS` must provide an adapter that can satisfy `IEngineWorldService` without leaking ECS types:

- `void Initialize()`
- `void Update(in EngineFrameContext context)`
- `void Shutdown()`
- `void Dispose()`

The adapter is expected to consume `EngineFrameContext.FrameNumber` and
`EngineFrameContext.DeltaSeconds` for deterministic simulation steps.

## Contracts requested from renderer owner

`DeltaRender` must provide an adapter that can satisfy `IEngineRenderService`:

- `void Initialize()`
- `void Render(in EngineFrameContext context)`
- `void Shutdown()`
- `void Dispose()`

The interface may be implemented by `DeltaRender` hosts that map
`EngineFrameContext` to frame extraction/buffering, as long as contract stage order
is preserved.

## Coexistence adapters

To keep current and new architecture compiling together, temporary adapters were added:

- `Source/DeltaEngine/Integration/LegacyAdapters.cs` (kept in the legacy project because its implementation types depend on the old runtime)
- `Source/DeltaEngine/DeltaEngine.csproj` references `Source/DeltaEngine.Integration/DeltaEngine.Integration.csproj` so those adapters consume the shared contracts without duplicating them.
- `SceneWorldAdapter` for `Delta.Runtime.Scene`
- `LegacyRenderAdapter` for existing graphics module API
- `LegacyInputAdapter`
- `LegacyUiAdapter`

These adapters are intentionally minimal and are only used as migration bridges until
`DeltaECS` and `DeltaRender` own the equivalent implementations.
