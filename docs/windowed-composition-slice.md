# Windowed composition slice

`Delta.Engine.Windowed` is an optional composition project. It is the owner of
the SDL3 event loop, input translation, close handling, resize observation and
frame clock. `EngineHost` still owns deterministic stage order:

```text
SDL3 poll -> InputSnapshot -> world update -> render frame -> UI producer update
```

The renderer receives surface metrics and frame time through
`EngineFrameContext`; it never polls SDL input. The headless `Delta.Engine`
boundary remains usable with `NullRenderer` and has no Render, Vulkan, SDL3 or
Shader project reference.

The sample uses the real `Delta.Render.Core` window/session contracts and
`Delta.Render.Vulkan` swapchain lifecycle. It loads the versioned vertex and
fragment `ShaderArtifact` outputs of Delta.Shader, creates a Vulkan graphics
pipeline, and draws the fullscreen SDF rectangle every frame. Resolution is
carried through Delta.Maths `float2`; elapsed time comes from `EngineHost`.

| Dependency | Current usable contract | Windowed slice status |
| --- | --- | --- |
| Delta.Maths | `float2` and related runtime value types | Used by SDF uniform description |
| Delta.Shader | C# vertex/fragment shaders and versioned graphics ABI | Generated fullscreen artifacts are consumed |
| Delta.Render | SDL3 window, Vulkan surface, swapchain and graphics pipeline | Fullscreen draw is connected |
| SDL3-CS | Native event polling/window operations | Owned by the platform shell |
| MoltenVK | Render sibling loads it on macOS | Available only in a real macOS display smoke |
| DeltaECS | Not required by this composition slice | World remains a neutral host service |

## Run

Build and test the optional composition project:

```bash
dotnet test Source/Delta.Engine.Windowed.Tests/Delta.Engine.Windowed.Tests.csproj -c Release
```

On macOS with a display, SDL3 and MoltenVK available, run one bounded frame:

```bash
dotnet run --project Source/Delta.Engine.Windowed/Delta.Engine.Windowed.csproj -c Release -- --frames 1
```

The next rendering step is to replace the fixed fullscreen parameters with a
renderer-neutral UI draw list and resource bindings; this does not require a
change to `EngineHost`, input ownership, or the headless core.

## Next UI layer

UI should be a producer of renderer-neutral data, not an Avalonia or renderer-
owned DOM. The smallest useful next contract is a per-frame draw list containing
rect, color, clip rectangle, and text runs referencing a text-atlas handle.
Editor and game can produce separate lists; one renderer can consume both. XAML
parsing/binding can be added above this list later without changing SDL ownership
or the render session lifecycle.
