# Compute artifact boundary

`Source/Delta.Engine.ComputeSmoke` owns orchestration only. It loads the
generated SPIR-V and ABI manifest pair into the runtime-neutral
`Delta.Shader.Abstractions.ShaderArtifact`; it does not duplicate shader
metadata or renderer pipeline contracts.

The sample does not reference `Delta.Shader.Compiler`, Roslyn, MSBuild, or
shader-generator runtime APIs. `compute_double.spv` is a pre-generated fixture;
the CPU world update, renderer subscription, SSBO upload, dispatch, readback,
and `i * 2 + 1` oracle remain runnable without shader compilation at runtime.

`Delta.Render` consumes the shared artifact through
`IComputeDevice.CreateComputePipeline(ShaderArtifact)` and derives its
renderer metadata from the manifest. The checked-in pair is generated output
for the smoke path; runtime Engine does not reference Roslyn, MSBuild, or the
shader compiler.
