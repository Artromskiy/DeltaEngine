using DeltaEngine.ECS;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DeltaEngine.Rendering;
internal static class ComponentMappers
{
    internal struct TrsData
    {
        public Vector4 position;
        public Quaternion rotation;
        public Vector4 scale;
    }

    internal struct RendData
    {
        public Guid shader;
        public Guid material;
        public Guid mesh;
    }

    internal struct TransformMapper : IGpuMapper<Transform, TrsData>
    {
        [MethodImpl(Inl)]
        public readonly TrsData Map(scoped ref Transform from) => new()
        {
            position = from._position,
            rotation = from.Rotation,
            scale = from._scale
        };
    }

    internal struct RenderMapper : IGpuMapper<Render, RendData>
    {
        public readonly RendData Map(scoped ref Render from) => new()
        {
            shader = from._shader.guid,
            material = from._material.guid,
            mesh = from.Mesh.guid
        };
    }

}
