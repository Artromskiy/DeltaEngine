using Arch.Core;
using DeltaEngine.Rendering;
using System.Numerics;

namespace DeltaEngine.ECS;
internal class RenderSystem
{
    private readonly GpuMappedSystem<Transform, Matrix4x4> _transforms;
    private readonly GpuMappedSystem<Render, uint> _renderers;

    public RenderSystem(World world, RenderBase renderBase)
    {
        _transforms = new GpuMappedSystem<Transform, Matrix4x4>(world, new TransformMapper(), renderBase);
        _renderers = new GpuMappedSystem<Render, uint>(world, new RenderMapper(), renderBase);
    }


    private struct TransformMapper : IGpuMapper<Transform, Matrix4x4>
    {
        public readonly Matrix4x4 Map(Transform from) => from.LocalMatrix;
    }

    private struct RenderMapper : IGpuMapper<Render, uint>
    {
        public readonly uint Map(Render from) => 0;
    }
}
