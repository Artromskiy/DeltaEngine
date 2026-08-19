using Delta.Render.Core;

namespace Delta.Engine.ComputeSmoke;

internal enum EngineComputeAccess
{
    ReadOnly,
    WriteOnly,
    ReadWrite,
}

internal readonly record struct EngineComputeBinding(
    uint Set,
    uint Binding,
    EngineComputeAccess Access);

/// <summary>
/// Sample-local neutral artifact boundary. It keeps shader bytes and execution
/// metadata together without depending on Delta.Shader compiler/Roslyn APIs.
/// </summary>
internal sealed record EngineComputeArtifact(
    ReadOnlyMemory<byte> Spirv,
    uint LocalSizeX,
    uint LocalSizeY,
    uint LocalSizeZ,
    IReadOnlyList<EngineComputeBinding> Bindings)
{
    public static EngineComputeArtifact LoadFixture(string shaderPath)
    {
        return new EngineComputeArtifact(
            File.ReadAllBytes(shaderPath),
            64,
            1,
            1,
            new[] { new EngineComputeBinding(0, 0, EngineComputeAccess.ReadWrite) });
    }

    public ComputeShaderMetadata ToRenderMetadata()
    {
        var bindings = Bindings
            .Select(binding => new ComputeDescriptorBinding(
                binding.Set,
                binding.Binding,
                ComputeDescriptorKind.StorageBuffer,
                binding.Access switch
                {
                    EngineComputeAccess.ReadOnly => ComputeBufferAccess.ReadOnly,
                    EngineComputeAccess.WriteOnly => ComputeBufferAccess.WriteOnly,
                    _ => ComputeBufferAccess.ReadWrite,
                }))
            .ToArray();

        return new ComputeShaderMetadata(
            ComputeAbiLayout.Std430,
            LocalSizeX,
            LocalSizeY,
            LocalSizeZ,
            bindings);
    }
}
