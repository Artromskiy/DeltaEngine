using Delta.Files;
using Delta.Rendering;
using Delta.Scripting;
using System;
using System.Runtime.CompilerServices;

namespace Delta.ECS.Components;

[Component]
public struct Render : IEquatable<Render>, IDirty, IComparable<Render>
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
            _shader = value.GetAsset().shader;
        }
    }

    [MethodImpl(Inl)]
    public readonly bool Equals(Render other) => _shader.Equals(other._shader) && _material.Equals(other._material) && Mesh.Equals(other.Mesh);
    [MethodImpl(Inl)]
    public override readonly bool Equals(object? obj) => obj is Render render && Equals(render);
    [MethodImpl(Inl)]
    public override readonly int GetHashCode() => HashCode.Combine(_shader, _material, Mesh);
    [MethodImpl(Inl)]
    public readonly int CompareTo(Render other)
    {
        var byShader = _shader.CompareTo(other._shader);
        var byMaterial = _material.CompareTo(other._material);
        var byMesh = Mesh.CompareTo(other.Mesh);
        return byShader == 0 ? byMaterial == 0 ? byMesh == 0 ? 1 : byMesh : byMaterial : byShader;
    }

    [MethodImpl(Inl)]
    public static bool operator ==(Render left, Render right)=> left.Equals(right);
    [MethodImpl(Inl)]
    public static bool operator !=(Render left, Render right)=> !(left == right);
}
