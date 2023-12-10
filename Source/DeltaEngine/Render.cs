using DeltaEngine.ECS;
using DeltaEngine.Files;
using DeltaEngine.Rendering;
using System.Runtime.CompilerServices;

namespace DeltaEngine;

internal struct Render : IDirty<Render>
{
    private GuidAsset<ShaderData> _shader;
    public GuidAsset<MaterialData> _material;
    public GuidAsset<MeshData> _mesh;

    public GuidAsset<ShaderData> Shader
    {
        [MethodImpl(Inl)]
        readonly get => _shader;
        [MethodImpl(Inl)]
        set => this.Set(ref _shader, ref value);
    }
    public GuidAsset<MaterialData> Material
    {
        [MethodImpl(Inl)]
        readonly get => _material;
        [MethodImpl(Inl)]
        set => this.Set(ref _material, ref value);
    }
    public GuidAsset<MeshData> Mesh
    {
        [MethodImpl(Inl)]
        readonly get => _mesh;
        [MethodImpl(Inl)]
        set => this.Set(ref _mesh, ref value);
    }

    bool IDirty.IsDirty { get; set; }
}
