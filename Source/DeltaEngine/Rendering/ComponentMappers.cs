using Delta.ECS;
using System;
using System.Numerics;

namespace Delta.Rendering;
internal static class ComponentMappers
{
    public struct TrsData
    {
        public Vector4 position;
        public Quaternion rotation;
        public Vector4 scale;
    }

    public struct RendData
    {
        public Guid shader;
        public Guid material;
        public Guid mesh;
    }

    public struct RenderMapper : IGpuMapper<Render, RendData>
    {
        public readonly RendData Map(scoped ref Render from) => new()
        {
            shader = from._shader.guid,
            material = from._material.guid,
            mesh = from.Mesh.guid
        };
    }
}
