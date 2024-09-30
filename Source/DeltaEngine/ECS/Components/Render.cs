using Delta.Assets;
using Delta.ECS.Attributes;
using System;

namespace Delta.ECS.Components;

[Component, Dirty]
public struct Render : IEquatable<Render>, IComparable<Render>
{
    internal GuidAsset<ShaderData> _shader;
    public GuidAsset<MaterialData> material;
    public GuidAsset<MeshData> mesh;

    public readonly GuidAsset<ShaderData> Shader
    {
        [Imp(Inl)]
        get => _shader;
    }

    public readonly bool IsValid => !_shader.Null && !material.Null && !mesh.Null;


    [Imp(Inl)]
    public readonly bool Equals(Render other) => _shader.Equals(other._shader) && material.Equals(other.material) && mesh.Equals(other.mesh);
    [Imp(Inl)]
    public override readonly bool Equals(object? obj) => obj is Render render && Equals(render);
    [Imp(Inl)]
    public override readonly int GetHashCode() => HashCode.Combine(_shader, material, mesh);
    [Imp(Inl)]
    public readonly int CompareTo(Render other)
    {
        var byShader = _shader.CompareTo(other._shader);
        var byMaterial = material.CompareTo(other.material);
        var byMesh = mesh.CompareTo(other.mesh);
        return byShader == 0 ? byMaterial == 0 ? byMesh == 0 ? 1 : byMesh : byMaterial : byShader;
    }

    [Imp(Inl)]
    public static bool operator ==(Render left, Render right) => left.Equals(right);
    [Imp(Inl)]
    public static bool operator !=(Render left, Render right) => !(left == right);
}
