# Headless renderer boundary

The legacy Vulkan/Silk renderer implementation has been removed from the
engine-owned core. `Delta.Engine.Integration` now provides the only required
renderer-neutral boundary:

- `IRenderFrameSink` accepts resize and frame data without backend types.
- `IRenderer` is the renderer-facing lifecycle contract.
- `NullRenderer` reports no backend and performs no GPU/window work.
- `NullGraphicsModule` drives deterministic resize and frame calls for
  headless engine/editor/game runs.

The core `Delta.Engine` project no longer references Vulkan, Silk.NET Vulkan,
Silk.NET Windowing, Delta.Shader, or `Delta.Render`. Runtime context creation uses
`NullGraphicsModule` for both headless and the currently-unimplemented
windowed path. SDL3-CS platform ownership remains outside this legacy core
layer and is not replaced here.

The optional `Source/Delta.Engine.ComputeSmoke` project is intentionally not in
the core solution. It references public `Delta.Render.Core` and
`Delta.Render.Vulkan` contracts only and can be built/run when the sibling
`DeltaRender` repository is available. Its absence must not affect core or
headless integration tests.

The removed renderer code included the old Vulkan device/swapchain/frame
implementation, render batchers, GPU collections, shader/pipeline helpers,
windowed/headless graphics modules, and `RenderStream` coupling. Scene/assets
and world-change contracts remain engine-owned data/API; backend adapters can
consume them later without reintroducing a core renderer dependency.
