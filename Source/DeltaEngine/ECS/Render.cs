using Delta.Files;
using Delta.Rendering;
using System;
using System.Runtime.CompilerServices;

namespace Delta.ECS;

internal struct Render : IEquatable<Render>, IDirty
{
    internal GuidAsset<ShaderData> _shader;
    internal GuidAsset<MaterialData> _material;
    public GuidAsset<MeshData> Mesh;

    public readonly GuidAsset<ShaderData> Shader
    {
        [MethodImpl(Inl)]
        get => _shader;
    }

    public GuidAsset<MaterialData> Material
    {
        [MethodImpl(Inl)]
        readonly get => _material;
        [MethodImpl(Inl)]
        set
        {
            _material = value;
            _shader = value.Asset.shader;
        }
    }

    [MethodImpl(Inl)]
    public readonly bool Equals(Render other) => _shader.Equals(other._shader) && _material.Equals(other._material) && Mesh.Equals(other.Mesh);
    [MethodImpl(Inl)]
    public override readonly bool Equals(object? obj) => obj is Render render && Equals(render);
    [MethodImpl(Inl)]
    public override readonly int GetHashCode() => HashCode.Combine(_shader, _material, Mesh);
}
