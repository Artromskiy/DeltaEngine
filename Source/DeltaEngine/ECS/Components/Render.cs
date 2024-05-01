using Delta.Files;
using Delta.Rendering;
using Delta.Scripting;
using System;
using System.Runtime.CompilerServices;

namespace Delta.ECS.Components;

[Dirty]
[Component]
public struct Render : IEquatable<Render>, IComparable<Render>
{
    internal GuidAsset<ShaderData> _shader;
    internal GuidAsset<MaterialData> _material;
    public GuidAsset<MeshData> mesh;

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
    public readonly bool Equals(Render other) => _shader.Equals(other._shader) && _material.Equals(other._material) && mesh.Equals(other.mesh);
    [MethodImpl(Inl)]
    public override readonly bool Equals(object? obj) => obj is Render render && Equals(render);
    [MethodImpl(Inl)]
    public override readonly int GetHashCode() => HashCode.Combine(_shader, _material, mesh);
    [MethodImpl(Inl)]
    public readonly int CompareTo(Render other)
    {
        var byShader = _shader.CompareTo(other._shader);
        var byMaterial = _material.CompareTo(other._material);
        var byMesh = mesh.CompareTo(other.mesh);
        return byShader == 0 ? byMaterial == 0 ? byMesh == 0 ? 1 : byMesh : byMaterial : byShader;
    }

    [MethodImpl(Inl)]
    public static bool operator ==(Render left, Render right) => left.Equals(right);
    [MethodImpl(Inl)]
    public static bool operator !=(Render left, Render right) => !(left == right);
}
