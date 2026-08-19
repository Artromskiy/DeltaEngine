# Compute artifact boundary

`Source/Delta.Engine.ComputeSmoke` owns orchestration only. Its
`EngineComputeArtifact` is a deliberately sample-local fixture adapter that
contains SPIR-V bytes, local workgroup size, and neutral storage-buffer binding
metadata. The adapter converts those values once to the current public
`Delta.Render.Core.ComputeShaderMetadata` contract.

The sample does not reference `Delta.Shader.Compiler`, Roslyn, MSBuild, or
shader-generator runtime APIs. `compute_double.spv` is a pre-generated fixture;
the CPU world update, renderer subscription, SSBO upload, dispatch, readback,
and `i * 2 + 1` oracle remain runnable without shader compilation at runtime.

`Delta.Shader` currently does not publish a runtime-neutral `ShaderArtifact`
type. When that contract is available, replace only `LoadFixture` and the
sample-local metadata conversion. The Engine runner and public Render compute
flow should remain unchanged.
