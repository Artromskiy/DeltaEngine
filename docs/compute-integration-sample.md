# Delta.Render compute integration sample

`Source/Delta.Engine.ComputeSmoke` is the first runnable engine-owned compute
path. It consumes the existing checked-in SPIR-V artifact used by the
Delta.Render compute smoke fixture; `fixtures/compute_double.comp` records the
shader source/provenance. No Vulkan implementation is duplicated in DeltaEngine.

There is currently no checked-in `.spv` artifact under GLSH in the workspace, so
this sample does not claim to run GLSH emission at build time. The exact future
GLSH hookup is the `byte[]`/`ReadOnlySpan<byte>` passed to
`IComputeDevice.CreateComputePipeline`; replacing the fixture producer there
does not require changing the renderer or the compute flow.

The sample uses only public `Delta.Render` contracts:

1. `VulkanRenderer.CreateComputeDevice()` creates the compute device.
2. `IComputeDevice.CreateStorageBuffer()` allocates the SSBO.
3. `IComputeDevice.Upload()` transfers engine input data.
4. `IComputeDevice.CreateComputePipeline()` consumes the SPIR-V bytes and
   `ComputeShaderMetadata`.
5. `IComputeDevice.Dispatch()` executes one workgroup.
6. `IComputeDevice.Readback()` returns the result for the oracle check.

The sample also uses `EngineWorldChangeJournal` in the same frame: the fake
world update records one component change per input value, the renderer consumes
only its subscription, then uploads, dispatches, and reads back. This keeps the
change contract tied to a real render path without exposing ECS or Vulkan types
through the engine integration boundary.

Run from the repository root with:

```text
dotnet run --project Source/Delta.Engine.ComputeSmoke/Delta.Engine.ComputeSmoke.csproj -c Release
```

The run requires a usable Vulkan compute device and the platform's Vulkan
loader. On macOS, `Delta.Render.Vulkan` owns the MoltenVK loading path.
